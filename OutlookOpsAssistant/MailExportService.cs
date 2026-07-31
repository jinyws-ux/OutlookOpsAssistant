using System;
using System.IO;
using System.Linq;
using Outlook = Microsoft.Office.Interop.Outlook;

namespace OutlookOpsAssistant
{
    public sealed class ExportedMailFile
    {
        public string FilePath { get; set; }

        public string FileName { get; set; }

        public long Size { get; set; }

        public DateTime ExportedAt { get; set; }
    }

    /// <summary>
    /// 只负责把当前 Outlook 邮件保存为本地文件。
    /// 上传到 Helix 的逻辑不放在这里。
    /// </summary>
    public interface IMailExporter
    {
        ExportedMailFile ExportCurrentMail(
            string exportDirectory);
    }

    public sealed class OutlookMsgExporter : IMailExporter
    {
        private readonly Outlook.Inspector inspector;

        public OutlookMsgExporter(
            Outlook.Inspector inspector)
        {
            this.inspector =
                inspector ??
                throw new ArgumentNullException(
                    nameof(inspector));
        }

        public ExportedMailFile ExportCurrentMail(
            string exportDirectory)
        {
            if (string.IsNullOrWhiteSpace(exportDirectory))
            {
                throw new ArgumentException(
                    "未配置邮件临时导出目录。",
                    nameof(exportDirectory));
            }

            Outlook.MailItem mailItem =
                inspector.CurrentItem as Outlook.MailItem;

            if (mailItem == null)
            {
                throw new InvalidOperationException(
                    "当前 Outlook 窗口中没有可导出的邮件。" );
            }

            Directory.CreateDirectory(exportDirectory);

            string subject =
                string.IsNullOrWhiteSpace(mailItem.Subject)
                    ? "OutlookMail"
                    : mailItem.Subject;

            string safeSubject =
                CreateSafeFileName(subject, 80);

            string fileName =
                DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") +
                "_" +
                safeSubject +
                ".msg";

            string filePath = Path.Combine(
                exportDirectory,
                fileName);

            mailItem.SaveAs(
                filePath,
                Outlook.OlSaveAsType.olMSGUnicode);

            FileInfo fileInfo =
                new FileInfo(filePath);

            if (!fileInfo.Exists ||
                fileInfo.Length <= 0)
            {
                throw new IOException(
                    "Outlook 未能生成有效的 MSG 文件。" );
            }

            return new ExportedMailFile
            {
                FilePath = fileInfo.FullName,
                FileName = fileInfo.Name,
                Size = fileInfo.Length,
                ExportedAt = DateTime.Now
            };
        }

        private static string CreateSafeFileName(
            string value,
            int maxLength)
        {
            char[] invalidChars =
                Path.GetInvalidFileNameChars();

            string safeValue =
                new string(
                    (value ?? string.Empty)
                    .Select(character =>
                        invalidChars.Contains(character)
                            ? '_'
                            : character)
                    .ToArray())
                .Trim()
                .TrimEnd('.');

            while (safeValue.Contains("  "))
            {
                safeValue = safeValue.Replace("  ", " ");
            }

            if (string.IsNullOrWhiteSpace(safeValue))
            {
                safeValue = "OutlookMail";
            }

            if (safeValue.Length > maxLength)
            {
                safeValue = safeValue.Substring(0, maxLength);
            }

            return safeValue;
        }
    }
}
