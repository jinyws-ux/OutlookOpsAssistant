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

        private CustomTaskPane taskPane;
        private bool disposed;

        public InspectorSession(
            Outlook.Inspector inspector)
        {
            this.inspector =
                inspector ??
                throw new ArgumentNullException(
                    nameof(inspector));

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

            taskPane.Width = 420;
            taskPane.Visible = false;

            inspectorEvents =
                (Outlook.InspectorEvents_Event)inspector;

            inspectorEvents.Close +=
                Inspector_Close;
        }

        public void AddFragment(
            Outlook.MailItem mailItem,
            string senderAddress,
            string selectedText)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(
                    nameof(InspectorSession));
            }

            paneControl.SetMailInfo(
                mailItem.Subject,
                senderAddress);

            paneControl.AddFragment(
                selectedText);

            taskPane.Visible = true;
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