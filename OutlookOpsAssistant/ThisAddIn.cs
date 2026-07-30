using System;
using System.Collections.Generic;
using Outlook = Microsoft.Office.Interop.Outlook;

namespace OutlookOpsAssistant
{
    public partial class ThisAddIn
    {
        private readonly Dictionary<
            Outlook.Inspector,
            InspectorSession> sessions =
            new Dictionary<
                Outlook.Inspector,
                InspectorSession>();

        private void ThisAddIn_Startup(
            object sender,
            EventArgs e)
        {
        }

        private void ThisAddIn_Shutdown(
            object sender,
            EventArgs e)
        {
            foreach (
                InspectorSession session
                in new List<InspectorSession>(
                    sessions.Values))
            {
                session.Dispose();
            }

            sessions.Clear();
        }

        public InspectorSession GetOrCreateSession(
            Outlook.Inspector inspector)
        {
            if (inspector == null)
            {
                throw new ArgumentNullException(
                    nameof(inspector));
            }

            if (!sessions.TryGetValue(
                    inspector,
                    out InspectorSession session))
            {
                session =
                    new InspectorSession(inspector);

                sessions.Add(
                    inspector,
                    session);
            }

            return session;
        }

        internal void RemoveSession(
            Outlook.Inspector inspector)
        {
            if (inspector == null)
            {
                return;
            }

            if (sessions.TryGetValue(
                    inspector,
                    out InspectorSession session))
            {
                sessions.Remove(inspector);
                session.Dispose();
            }
        }

        #region VSTO 生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InternalStartup()
        {
            this.Startup += new System.EventHandler(ThisAddIn_Startup);
            this.Shutdown += new System.EventHandler(ThisAddIn_Shutdown);
        }
        
        #endregion
    }
}
