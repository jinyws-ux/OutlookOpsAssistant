using System;
using System.Windows.Forms;
using Microsoft.Office.Tools.Ribbon;
using Outlook = Microsoft.Office.Interop.Outlook;

namespace OutlookOpsAssistant
{
    public partial class OpsRibbon
    {
        private void OpsRibbon_Load(
            object sender,
            RibbonUIEventArgs e)
        {
            // 暂时不需要初始化操作
        }

        private void button1_Click(
            object sender,
            RibbonControlEventArgs e)
        {
            try
            {
                Outlook.Application outlookApp =
                    Globals.ThisAddIn.Application;

                Outlook.Inspector inspector =
                    outlookApp.ActiveInspector();

                if (inspector == null)
                {
                    MessageBox.Show(
                        "没有检测到独立邮件窗口。\n\n" +
                        "请先双击打开一封收到的邮件。",
                        "Outlook 运维助手",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    return;
                }

                Outlook.MailItem mailItem =
                    inspector.CurrentItem as Outlook.MailItem;

                if (mailItem == null)
                {
                    MessageBox.Show(
                        "当前窗口不是普通邮件。",
                        "Outlook 运维助手",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return;
                }

                string selectedText =
                    GetSelectedText(inspector);

                if (string.IsNullOrWhiteSpace(selectedText))
                {
                    MessageBox.Show(
                        "没有读取到选中文字。\n\n" +
                        "请在邮件正文中选中一段文字，" +
                        "然后再次点击“加入关键内容”。",
                        "Outlook 运维助手",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    return;
                }

                string senderAddress =
                    GetSenderAddress(mailItem);

                // 获取当前邮件窗口对应的任务窗格
                InspectorSession session =
                    Globals.ThisAddIn.GetOrCreateSession(
                        inspector);

                // 将当前选中的文字加入右侧任务窗格
                session.AddFragment(
                    mailItem,
                    senderAddress,
                    selectedText);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "读取邮件失败：\n\n" +
                    ex.Message +
                    "\n\n异常类型：\n" +
                    ex.GetType().FullName,
                    "Outlook 运维助手",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 获取运维人员当前在邮件正文中选中的文字。
        /// Outlook 邮件正文实际由 Word 编辑器呈现。
        /// </summary>
        private static string GetSelectedText(
            Outlook.Inspector inspector)
        {
            dynamic wordDocument =
                inspector.WordEditor;

            if (wordDocument == null)
            {
                return string.Empty;
            }

            dynamic wordApplication =
                wordDocument.Application;

            dynamic selection =
                wordApplication.Selection;

            if (selection == null)
            {
                return string.Empty;
            }

            string text =
                Convert.ToString(selection.Text) ??
                string.Empty;

            /*
             * Word 表格单元格结尾可能包含：
             * \r：换行
             * \a：单元格结束控制符
             */
            text = text
                .Replace("\r\a", Environment.NewLine)
                .Replace("\a", string.Empty)
                .Trim();

            return text;
        }

        /// <summary>
        /// 获取邮件发件人的 SMTP 邮箱地址。
        /// 公司内部 Exchange 邮件需要单独转换。
        /// </summary>
        private static string GetSenderAddress(
            Outlook.MailItem mailItem)
        {
            try
            {
                if (string.Equals(
                    mailItem.SenderEmailType,
                    "EX",
                    StringComparison.OrdinalIgnoreCase))
                {
                    Outlook.AddressEntry addressEntry =
                        mailItem.Sender;

                    if (addressEntry != null)
                    {
                        Outlook.ExchangeUser exchangeUser =
                            addressEntry.GetExchangeUser();

                        if (exchangeUser != null &&
                            !string.IsNullOrWhiteSpace(
                                exchangeUser.PrimarySmtpAddress))
                        {
                            return exchangeUser
                                .PrimarySmtpAddress;
                        }
                    }
                }

                return mailItem.SenderEmailAddress ??
                       string.Empty;
            }
            catch
            {
                return mailItem.SenderEmailAddress ??
                       string.Empty;
            }
        }
    }
}