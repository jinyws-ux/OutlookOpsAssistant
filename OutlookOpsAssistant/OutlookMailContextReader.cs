using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Outlook = Microsoft.Office.Interop.Outlook;

namespace OutlookOpsAssistant
{
    /// <summary>
    /// 负责把 Outlook MailItem 转换成与 Outlook 解耦的 MailContext。
    /// </summary>
    public sealed class OutlookMailContextReader
    {
        private static readonly string[] HistoryMarkers =
        {
            "-----Original Message-----",
            "-----原始邮件-----",
            "发件人:",
            "发件人：",
            "From:",
            "From："
        };

        public MailContext Read(
            Outlook.MailItem mailItem)
        {
            if (mailItem == null)
            {
                throw new ArgumentNullException(
                    nameof(mailItem));
            }

            string body =
                SafeGet(() => mailItem.Body) ??
                string.Empty;

            MailContext context =
                new MailContext
                {
                    EntryId =
                        SafeGet(() => mailItem.EntryID) ??
                        string.Empty,
                    Subject =
                        SafeGet(() => mailItem.Subject) ??
                        string.Empty,
                    SenderName =
                        SafeGet(() => mailItem.SenderName) ??
                        string.Empty,
                    SenderAddress =
                        GetSenderAddress(mailItem),
                    To =
                        SafeGet(() => mailItem.To) ??
                        string.Empty,
                    Cc =
                        SafeGet(() => mailItem.CC) ??
                        string.Empty,
                    FullBody = body,
                    LatestContent =
                        ExtractLatestContent(body),
                    ReceivedTime =
                        SafeGetNullableDateTime(
                            () => mailItem.ReceivedTime)
                };

            ReadAttachments(
                mailItem,
                context.Attachments);

            return context;
        }

        private static void ReadAttachments(
            Outlook.MailItem mailItem,
            IList<MailAttachmentInfo> target)
        {
            Outlook.Attachments attachments = null;

            try
            {
                attachments = mailItem.Attachments;

                if (attachments == null)
                {
                    return;
                }

                for (int index = 1;
                     index <= attachments.Count;
                     index++)
                {
                    Outlook.Attachment attachment = null;

                    try
                    {
                        attachment =
                            attachments[index];

                        target.Add(
                            new MailAttachmentInfo
                            {
                                FileName =
                                    SafeGet(
                                        () => attachment.FileName) ??
                                    string.Empty,
                                Size =
                                    SafeGetLong(
                                        () => attachment.Size),
                                AttachmentType =
                                    SafeGet(
                                        () => attachment.Type.ToString()) ??
                                    string.Empty
                            });
                    }
                    finally
                    {
                        ReleaseComObject(attachment);
                    }
                }
            }
            catch
            {
                // 单个邮件附件读取失败时，不阻断邮件分析主流程。
            }
            finally
            {
                ReleaseComObject(attachments);
            }
        }

        private static string ExtractLatestContent(
            string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return string.Empty;
            }

            string normalized =
                body
                    .Replace("\r\n", "\n")
                    .Replace('\r', '\n');

            string[] lines =
                normalized.Split('\n');

            List<string> latestLines =
                new List<string>();

            foreach (string line in lines)
            {
                string trimmed =
                    (line ?? string.Empty).Trim();

                if (latestLines.Count > 0 &&
                    IsHistoryMarker(trimmed))
                {
                    break;
                }

                latestLines.Add(line ?? string.Empty);
            }

            string latest =
                string.Join(
                    Environment.NewLine,
                    latestLines)
                .Trim();

            return string.IsNullOrWhiteSpace(latest)
                ? body.Trim()
                : latest;
        }

        private static bool IsHistoryMarker(
            string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return false;
            }

            foreach (string marker in HistoryMarkers)
            {
                if (line.StartsWith(
                        marker,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetSenderAddress(
            Outlook.MailItem mailItem)
        {
            Outlook.AddressEntry addressEntry = null;
            Outlook.ExchangeUser exchangeUser = null;

            try
            {
                string senderEmailType =
                    SafeGet(
                        () => mailItem.SenderEmailType) ??
                    string.Empty;

                if (string.Equals(
                        senderEmailType,
                        "EX",
                        StringComparison.OrdinalIgnoreCase))
                {
                    addressEntry = mailItem.Sender;

                    if (addressEntry != null)
                    {
                        exchangeUser =
                            addressEntry.GetExchangeUser();

                        if (exchangeUser != null &&
                            !string.IsNullOrWhiteSpace(
                                exchangeUser.PrimarySmtpAddress))
                        {
                            return exchangeUser.PrimarySmtpAddress;
                        }
                    }
                }

                return
                    SafeGet(
                        () => mailItem.SenderEmailAddress) ??
                    string.Empty;
            }
            catch
            {
                return
                    SafeGet(
                        () => mailItem.SenderEmailAddress) ??
                    string.Empty;
            }
            finally
            {
                ReleaseComObject(exchangeUser);
                ReleaseComObject(addressEntry);
            }
        }

        private static string SafeGet(
            Func<string> getter)
        {
            try
            {
                return getter();
            }
            catch
            {
                return string.Empty;
            }
        }

        private static long SafeGetLong(
            Func<int> getter)
        {
            try
            {
                return getter();
            }
            catch
            {
                return 0;
            }
        }

        private static DateTime? SafeGetNullableDateTime(
            Func<DateTime> getter)
        {
            try
            {
                return getter();
            }
            catch
            {
                return null;
            }
        }

        private static void ReleaseComObject(
            object value)
        {
            if (value == null ||
                !Marshal.IsComObject(value))
            {
                return;
            }

            try
            {
                Marshal.FinalReleaseComObject(value);
            }
            catch
            {
                // Outlook 关闭或 COM 对象已释放时忽略。
            }
        }
    }
}
