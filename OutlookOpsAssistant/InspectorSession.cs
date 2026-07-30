using System;
using Microsoft.Office.Tools;
using Office = Microsoft.Office.Core;
using Outlook = Microsoft.Office.Interop.Outlook;

namespace OutlookOpsAssistant
{
    public sealed class InspectorSession : IDisposable
    {
        private readonly Outlook.Inspector inspector;
        private readonly Outlook.InspectorEvents_Event inspectorEvents;
        private readonly OpsTaskPaneControl paneControl;
        private readonly OutlookMailContextReader mailContextReader;

        private CustomTaskPane taskPane;
        private bool disposed;

        public InspectorSession(
            Outlook.Inspector inspector)
        {
            this.inspector =
                inspector ??
                throw new ArgumentNullException(
                    nameof(inspector));

            mailContextReader =
                new OutlookMailContextReader();

            paneControl =
                new OpsTaskPaneControl();

            taskPane =
                Globals.ThisAddIn.CustomTaskPanes.Add(
                    paneControl,
                    "运维自动化",
                    inspector);

            taskPane.DockPosition =
                Office.MsoCTPDockPosition
                    .msoCTPDockPositionRight;

            taskPane.Width = 440;
            taskPane.Visible = false;

            inspectorEvents =
                (Outlook.InspectorEvents_Event)inspector;

            inspectorEvents.Close +=
                Inspector_Close;
        }

        /// <summary>
        /// 读取当前完整邮件并打开右侧分析窗格。
        /// </summary>
        public void OpenAndAnalyze(
            Outlook.MailItem mailItem)
        {
            EnsureNotDisposed();

            MailContext context =
                mailContextReader.Read(mailItem);

            paneControl.LoadMailContext(context);
            taskPane.Visible = true;
        }

        /// <summary>
        /// 保留原有的选中文字补充能力，后续作为次要入口使用。
        /// </summary>
        public void AddFragment(
            Outlook.MailItem mailItem,
            string selectedText)
        {
            EnsureNotDisposed();

            MailContext context =
                mailContextReader.Read(mailItem);

            paneControl.LoadMailContext(context);
            paneControl.AddFragment(selectedText);
            taskPane.Visible = true;
        }

        private void EnsureNotDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(
                    nameof(InspectorSession));
            }
        }

        private void Inspector_Close()
        {
            Globals.ThisAddIn.RemoveSession(
                inspector);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;

            try
            {
                inspectorEvents.Close -=
                    Inspector_Close;
            }
            catch
            {
                // Outlook关闭阶段可能已经释放事件源。
            }

            if (taskPane != null)
            {
                try
                {
                    Globals.ThisAddIn.CustomTaskPanes.Remove(
                        taskPane);
                }
                catch
                {
                    // Outlook关闭阶段忽略清理异常。
                }

                taskPane = null;
            }
        }
    }
}
