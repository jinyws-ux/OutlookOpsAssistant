using System;
using System.Collections.Generic;

namespace OutlookOpsAssistant
{
    public sealed class CaseSuggestion
    {
        public string CaseCode { get; set; }

        public string CaseName { get; set; }

        public double Score { get; set; }

        public string Reason { get; set; }

        public Dictionary<string, string>
            ExtractedParameters { get; set; }

        public CaseSuggestion()
        {
            ExtractedParameters =
                new Dictionary<string, string>();
        }

        public override string ToString()
        {
            return string.Format(
                "{0}  {1:P0}",
                CaseName,
                Score);
        }
    }

    public sealed class MailAnalysisResult
    {
        public string Summary { get; set; }

        public List<CaseSuggestion> Candidates { get; set; }

        public MailAnalysisResult()
        {
            Candidates = new List<CaseSuggestion>();
        }
    }

    public interface IAgentService
    {
        MailAnalysisResult AnalyzeMail(
            MailContext context,
            IList<CaseDefinition> caseDefinitions);

        IDictionary<string, string> ExtractParameters(
            MailContext context,
            CaseDefinition caseDefinition);
    }

    /// <summary>
    /// 防止配置为真实 Agent 后仍静默调用 Mock。
    /// HTTP Agent 接入完成前会明确提示当前模式尚不可用。
    /// </summary>
    public sealed class UnavailableAgentService : IAgentService
    {
        private readonly string configuredMode;

        public UnavailableAgentService(string configuredMode)
        {
            this.configuredMode =
                string.IsNullOrWhiteSpace(configuredMode)
                    ? "unknown"
                    : configuredMode;
        }

        public MailAnalysisResult AnalyzeMail(
            MailContext context,
            IList<CaseDefinition> caseDefinitions)
        {
            throw CreateException();
        }

        public IDictionary<string, string> ExtractParameters(
            MailContext context,
            CaseDefinition caseDefinition)
        {
            throw CreateException();
        }

        private InvalidOperationException CreateException()
        {
            return new InvalidOperationException(
                "当前配置的 Agent 模式为 " +
                configuredMode +
                "，但此版本尚未接入该模式。请暂时改为 mock。" );
        }
    }
}
