using System;
using System.Collections.Generic;

namespace OutlookOpsAssistant
{
    public sealed class CaseApiExecutionResult
    {
        public bool Success { get; set; }

        public string Message { get; set; }

        public Dictionary<string, object> Data { get; set; }

        public CaseApiExecutionResult()
        {
            Data = new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Case API 只负责执行具体业务动作，
    /// 与 Helix 开单和 Outlook 邮件导出相互独立。
    /// </summary>
    public interface ICaseApiService
    {
        CaseApiExecutionResult Execute(
            CaseDefinition caseDefinition,
            TicketCreateRequest request,
            string ticketId);
    }

    public sealed class MockCaseApiService : ICaseApiService
    {
        public CaseApiExecutionResult Execute(
            CaseDefinition caseDefinition,
            TicketCreateRequest request,
            string ticketId)
        {
            if (caseDefinition == null)
            {
                throw new ArgumentNullException(
                    nameof(caseDefinition));
            }

            if (request == null)
            {
                throw new ArgumentNullException(
                    nameof(request));
            }

            CaseApiExecutionResult result =
                new CaseApiExecutionResult
                {
                    Success = true,
                    Message =
                        "模拟 Case API 执行成功：" +
                        caseDefinition.Name
                };

            foreach (KeyValuePair<string, string> pair
                     in request.Parameters)
            {
                result.Data[pair.Key] = pair.Value;
            }

            result.Data["caseApiStatus"] =
                "模拟执行成功";

            result.Data["caseApiEndpoint"] =
                caseDefinition.Api == null
                    ? string.Empty
                    : caseDefinition.Api.Endpoint;

            result.Data["ticketId"] =
                ticketId ?? string.Empty;

            return result;
        }
    }

    /// <summary>
    /// 真实 HTTP 调用会在确认各 Case 的请求、认证和回参协议后实现。
    /// 当前不对未知生产接口发送请求。
    /// </summary>
    public sealed class UnavailableCaseApiService : ICaseApiService
    {
        private readonly string mode;

        public UnavailableCaseApiService(
            string mode)
        {
            this.mode =
                string.IsNullOrWhiteSpace(mode)
                    ? "http"
                    : mode;
        }

        public CaseApiExecutionResult Execute(
            CaseDefinition caseDefinition,
            TicketCreateRequest request,
            string ticketId)
        {
            return new CaseApiExecutionResult
            {
                Success = false,
                Message =
                    "Case API 模式已设置为 " +
                    mode +
                    "，但真实请求协议尚未完成对接。"
            };
        }
    }
}
