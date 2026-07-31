using System;
using System.Collections.Generic;
using System.IO;

namespace OutlookOpsAssistant
{
    public sealed class HelixTicketCreateResult
    {
        public bool Success { get; set; }

        public string TicketId { get; set; }

        public string Message { get; set; }

        public Dictionary<string, object> Data { get; set; }

        public HelixTicketCreateResult()
        {
            Data = new Dictionary<string, object>();
        }
    }

    public sealed class HelixAttachmentUploadResult
    {
        public bool Success { get; set; }

        public string AttachmentId { get; set; }

        public string Message { get; set; }
    }

    /// <summary>
    /// Helix 只负责创建工单和上传工单附件。
    /// Case 业务执行不放在该接口中。
    /// </summary>
    public interface IHelixService
    {
        HelixTicketCreateResult CreateTicket(
            TicketCreateRequest request);

        HelixAttachmentUploadResult UploadAttachment(
            string ticketId,
            string filePath);
    }

    public sealed class MockHelixService : IHelixService
    {
        public HelixTicketCreateResult CreateTicket(
            TicketCreateRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(
                    nameof(request));
            }

            string ticketId =
                "TEST-INC-" +
                DateTime.Now.ToString(
                    "yyyyMMddHHmmssfff");

            return new HelixTicketCreateResult
            {
                Success = true,
                TicketId = ticketId,
                Message = "模拟 Helix 工单创建成功"
            };
        }

        public HelixAttachmentUploadResult UploadAttachment(
            string ticketId,
            string filePath)
        {
            if (string.IsNullOrWhiteSpace(ticketId))
            {
                return new HelixAttachmentUploadResult
                {
                    Success = false,
                    Message = "缺少工单号，无法上传附件。"
                };
            }

            if (string.IsNullOrWhiteSpace(filePath) ||
                !File.Exists(filePath))
            {
                return new HelixAttachmentUploadResult
                {
                    Success = false,
                    Message = "待上传的邮件文件不存在。"
                };
            }

            return new HelixAttachmentUploadResult
            {
                Success = true,
                AttachmentId =
                    "TEST-ATT-" +
                    DateTime.Now.ToString(
                        "yyyyMMddHHmmssfff"),
                Message = "模拟 Helix 邮件附件上传成功"
            };
        }
    }

    /// <summary>
    /// 已经完成接口隔离，但在取得 Helix 字段映射前不发送真实请求。
    /// 避免在生产环境误用未经确认的请求结构。
    /// </summary>
    public sealed class UnavailableHelixService : IHelixService
    {
        private readonly string mode;

        public UnavailableHelixService(
            string mode)
        {
            this.mode =
                string.IsNullOrWhiteSpace(mode)
                    ? "http"
                    : mode;
        }

        public HelixTicketCreateResult CreateTicket(
            TicketCreateRequest request)
        {
            return new HelixTicketCreateResult
            {
                Success = false,
                Message =
                    "Helix 模式已设置为 " +
                    mode +
                    "，但真实 Helix 字段映射尚未配置。"
            };
        }

        public HelixAttachmentUploadResult UploadAttachment(
            string ticketId,
            string filePath)
        {
            return new HelixAttachmentUploadResult
            {
                Success = false,
                Message =
                    "真实 Helix 附件接口尚未完成对接。"
            };
        }
    }
}
