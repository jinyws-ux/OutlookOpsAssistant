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
            // 暂时不需要初始化操作。
        }

        private void AnalyzeMailButton_Click(
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

                InspectorSession session =
                    Globals.ThisAddIn.GetOrCreateSession(
                        inspector);

                session.OpenAndAnalyze(mailItem);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "分析邮件失败：\n\n" +
                    ex.Message +
                    "\n\n异常类型：\n" +
                    ex.GetType().FullName,
                    "Outlook 运维助手",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
