using System;
using System.Collections.Generic;

namespace OutlookOpsAssistant
{
    /// <summary>
    /// 插件从当前 Outlook 邮件中读取出的统一上下文。
    /// 后续 Mock Agent、Dify 或其他 Agent 都只依赖该模型，
    /// 不直接依赖 Outlook COM 对象。
    /// </summary>
    public sealed class MailContext
    {
        public string EntryId { get; set; }

        public string Subject { get; set; }

        public string SenderName { get; set; }

        public string SenderAddress { get; set; }

        public string To { get; set; }

        public string Cc { get; set; }

        public string FullBody { get; set; }

        public string LatestContent { get; set; }

        public DateTime? ReceivedTime { get; set; }

        public List<MailAttachmentInfo> Attachments { get; set; }

        public MailContext()
        {
            Attachments = new List<MailAttachmentInfo>();
        }
    }

    public sealed class MailAttachmentInfo
    {
        public string FileName { get; set; }

        public long Size { get; set; }

        public string AttachmentType { get; set; }
    }
}
