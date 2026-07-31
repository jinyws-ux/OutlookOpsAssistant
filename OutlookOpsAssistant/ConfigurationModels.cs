using System;
using System.Collections.Generic;
using System.Text;

namespace OutlookOpsAssistant
{
    public sealed class BootstrapConfiguration
    {
        public string Environment { get; set; }

        public string CaseConfigPath { get; set; }

        public AgentConfiguration Agent { get; set; }

        public ApiConfiguration Api { get; set; }

        public RuntimePathConfiguration Paths { get; set; }

        public BootstrapConfiguration()
        {
            Environment = "test";
            Agent = new AgentConfiguration();
            Api = new ApiConfiguration();
            Paths = new RuntimePathConfiguration();
        }
    }

    public sealed class AgentConfiguration
    {
        public string Mode { get; set; }

        public string ClassificationEndpoint { get; set; }

        public string ExtractionEndpoint { get; set; }

        public int TimeoutSeconds { get; set; }

        public AgentConfiguration()
        {
            Mode = "mock";
            TimeoutSeconds = 30;
        }
    }

    public sealed class ApiConfiguration
    {
        public bool Enabled { get; set; }

        public string BaseUrl { get; set; }

        public int TimeoutSeconds { get; set; }

        public ApiConfiguration()
        {
            TimeoutSeconds = 30;
        }
    }

    public sealed class RuntimePathConfiguration
    {
        public string CacheDirectory { get; set; }

        public string LogDirectory { get; set; }
    }

    public sealed class CaseConfigurationDocument
    {
        public int Version { get; set; }

        public List<CaseConfigurationItem> Cases { get; set; }

        public CaseConfigurationDocument()
        {
            Version = 1;
            Cases = new List<CaseConfigurationItem>();
        }
    }

    public sealed class CaseConfigurationItem
    {
        public bool? Enabled { get; set; }

        public string Code { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string SummaryTemplate { get; set; }

        public bool AutomationEnabled { get; set; }

        public List<string> MatchKeywords { get; set; }

        public List<CaseFieldConfiguration> Fields { get; set; }

        public List<CaseResultFieldConfiguration> ResultDisplay { get; set; }

        public CaseApiConfiguration Api { get; set; }

        public CaseConfigurationItem()
        {
            MatchKeywords = new List<string>();
            Fields = new List<CaseFieldConfiguration>();
            ResultDisplay = new List<CaseResultFieldConfiguration>();
            Api = new CaseApiConfiguration();
        }
    }

    public sealed class CaseFieldConfiguration
    {
        public string Key { get; set; }

        public string Label { get; set; }

        public string Type { get; set; }

        public bool Required { get; set; }

        public string DefaultValue { get; set; }

        public List<string> Options { get; set; }

        public CaseFieldConfiguration()
        {
            Type = "text";
            Options = new List<string>();
        }
    }

    public sealed class CaseResultFieldConfiguration
    {
        public string Label { get; set; }

        public string Path { get; set; }

        public string Type { get; set; }

        public bool Copyable { get; set; }

        public CaseResultFieldConfiguration()
        {
            Type = "text";
        }
    }

    public sealed class CaseApiConfiguration
    {
        public bool Enabled { get; set; }

        public string Endpoint { get; set; }

        public string Method { get; set; }

        public int TimeoutSeconds { get; set; }

        public CaseApiConfiguration()
        {
            Method = "POST";
            TimeoutSeconds = 30;
        }
    }

    public sealed class RuntimeConfigurationSnapshot
    {
        public string EnvironmentName { get; set; }

        public string BootstrapPath { get; set; }

        public string CaseConfigPath { get; set; }

        public string CaseConfigSource { get; set; }

        public string CacheDirectory { get; set; }

        public string LogDirectory { get; set; }

        public AgentConfiguration Agent { get; set; }

        public ApiConfiguration Api { get; set; }

        public IList<CaseDefinition> Cases { get; set; }

        public IList<string> Warnings { get; set; }

        public bool UsedFallbackCases { get; set; }

        public DateTime LoadedAt { get; set; }

        public RuntimeConfigurationSnapshot()
        {
            Agent = new AgentConfiguration();
            Api = new ApiConfiguration();
            Cases = new List<CaseDefinition>();
            Warnings = new List<string>();
            LoadedAt = DateTime.Now;
        }

        public string BuildDiagnosticText()
        {
            StringBuilder text = new StringBuilder();

            text.AppendLine("环境：" + ValueOrDash(EnvironmentName));
            text.AppendLine("加载时间：" + LoadedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            text.AppendLine();
            text.AppendLine("Bootstrap：" + ValueOrDash(BootstrapPath));
            text.AppendLine("Case 配置：" + ValueOrDash(CaseConfigPath));
            text.AppendLine("Case 来源：" + ValueOrDash(CaseConfigSource));
            text.AppendLine("已启用 Case：" + (Cases == null ? 0 : Cases.Count));
            text.AppendLine();
            text.AppendLine("Agent 模式：" + ValueOrDash(Agent == null ? null : Agent.Mode));
            text.AppendLine("分类地址：" + ValueOrDash(Agent == null ? null : Agent.ClassificationEndpoint));
            text.AppendLine("参数提取地址：" + ValueOrDash(Agent == null ? null : Agent.ExtractionEndpoint));
            text.AppendLine();
            text.AppendLine("API 开关：" + ((Api != null && Api.Enabled) ? "启用" : "关闭"));
            text.AppendLine("API 基础地址：" + ValueOrDash(Api == null ? null : Api.BaseUrl));
            text.AppendLine("缓存目录：" + ValueOrDash(CacheDirectory));
            text.AppendLine("日志目录：" + ValueOrDash(LogDirectory));

            if (Warnings != null && Warnings.Count > 0)
            {
                text.AppendLine();
                text.AppendLine("提示：");

                foreach (string warning in Warnings)
                {
                    text.AppendLine("- " + warning);
                }
            }

            return text.ToString().TrimEnd();
        }

        private static string ValueOrDash(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "-"
                : value;
        }
    }
}
