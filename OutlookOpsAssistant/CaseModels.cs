using System.Collections.Generic;

namespace OutlookOpsAssistant
{
    public enum CaseFieldType
    {
        Text,
        MultiLineText,
        Select
    }

    public sealed class CaseFieldDefinition
    {
        public string Key { get; set; }

        public string Label { get; set; }

        public CaseFieldType FieldType { get; set; }

        public bool Required { get; set; }

        public string DefaultValue { get; set; }

        public List<string> Options { get; set; }

        public CaseFieldDefinition()
        {
            Options = new List<string>();
        }
    }

    public sealed class CaseDefinition
    {
        public string Code { get; set; }

        public string Name { get; set; }

        public string SummaryTemplate { get; set; }

        public bool AutomationEnabled { get; set; }

        public List<CaseFieldDefinition> Fields { get; set; }

        public CaseDefinition()
        {
            Fields = new List<CaseFieldDefinition>();
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public sealed class TicketCreateRequest
    {
        public string CaseCode { get; set; }

        public string CaseName { get; set; }

        public string MailSubject { get; set; }

        public string Sender { get; set; }

        public string TicketSummary { get; set; }

        public string Description { get; set; }

        public bool AutomationEnabled { get; set; }

        public Dictionary<string, string> Parameters { get; set; }

        public List<string> SelectedFragments { get; set; }

        public TicketCreateRequest()
        {
            Parameters = new Dictionary<string, string>();
            SelectedFragments = new List<string>();
        }
    }

    public sealed class TicketCreateResult
    {
        public bool Success { get; set; }

        public string TicketId { get; set; }

        public string Message { get; set; }
    }
}