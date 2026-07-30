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

    /// <summary>
    /// 插件只依赖该接口。
    /// 后续接入 Dify、RAGFlow 或其他 Agent 时，
    /// 替换实现类即可，不需要修改 Outlook 读取和界面主流程。
    /// </summary>
    public interface IAgentService
    {
        MailAnalysisResult AnalyzeMail(
            MailContext context,
            IList<CaseDefinition> caseDefinitions);

        IDictionary<string, string> ExtractParameters(
            MailContext context,
            CaseDefinition caseDefinition);
    }
}
