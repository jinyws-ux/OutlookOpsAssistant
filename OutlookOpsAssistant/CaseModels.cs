using System;
using System.Collections.Generic;

namespace OutlookOpsAssistant
{
    public enum CaseFieldType
    {
        Text,
        MultiLineText,
        Select
    }

    public enum CaseResultDisplayType
    {
        Text,
        Status,
        MultiLine,
        List,
        Table
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

    public sealed class CaseResultFieldDefinition
    {
        public string Label { get; set; }

        public string Path { get; set; }

        public CaseResultDisplayType DisplayType { get; set; }

        public bool Copyable { get; set; }
    }

    public sealed class CaseApiDefinition
    {
        public bool Enabled { get; set; }

        public string Endpoint { get; set; }

        public string Method { get; set; }

        public int TimeoutSeconds { get; set; }

        public CaseApiDefinition()
        {
            Method = "POST";
            TimeoutSeconds = 30;
        }
    }

    public sealed class CaseDefinition
    {
        public bool Enabled { get; set; }

        public string Code { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string SummaryTemplate { get; set; }

        public bool AutomationEnabled { get; set; }

        public List<string> MatchKeywords { get; set; }

        public List<CaseFieldDefinition> Fields { get; set; }

        public List<CaseResultFieldDefinition> ResultDisplay { get; set; }

        public CaseApiDefinition Api { get; set; }

        public CaseDefinition()
        {
            Enabled = true;
            MatchKeywords = new List<string>();
            Fields = new List<CaseFieldDefinition>();
            ResultDisplay = new List<CaseResultFieldDefinition>();
            Api = new CaseApiDefinition();
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

        public TicketCreateRequest()
        {
            Parameters = new Dictionary<string, string>();
        }
    }

    public sealed class TicketCreateResult
    {
        public bool Success { get; set; }

        public string TicketId { get; set; }

        public string Message { get; set; }

        public DateTime CompletedAt { get; set; }

        public Dictionary<string, object> Data { get; set; }

        public TicketCreateResult()
        {
            Data = new Dictionary<string, object>();
        }
    }
}
