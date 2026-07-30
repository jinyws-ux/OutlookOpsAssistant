using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OutlookOpsAssistant
{
    /// <summary>
    /// 开发阶段使用的本地模拟 Agent。
    /// 不访问外部 AI 服务，便于先跑通插件完整流程。
    /// </summary>
    public sealed class MockAgentService : IAgentService
    {
        private static readonly Regex LabeledVinRegex =
            new Regex(
                @"(?:SHORT[\s_-]*VIN|VIN(?:号|号码)?)\s*[:：=]?\s*([MS1][A-Z0-9]{6})(?![A-Z0-9])",
                RegexOptions.IgnoreCase |
                RegexOptions.Compiled);

        private static readonly Regex VinTokenRegex =
            new Regex(
                @"(?<![A-Z0-9])([MS1][A-Z0-9]{6})(?![A-Z0-9])",
                RegexOptions.IgnoreCase |
                RegexOptions.Compiled);

        public MailAnalysisResult AnalyzeMail(
            MailContext context,
            IList<CaseDefinition> caseDefinitions)
        {
            if (context == null)
            {
                throw new ArgumentNullException(
                    nameof(context));
            }

            if (caseDefinitions == null)
            {
                throw new ArgumentNullException(
                    nameof(caseDefinitions));
            }

            string searchText =
                BuildSearchText(context);

            List<CaseSuggestion> suggestions =
                new List<CaseSuggestion>();

            foreach (CaseDefinition caseDefinition
                     in caseDefinitions)
            {
                if (caseDefinition == null)
                {
                    continue;
                }

                suggestions.Add(
                    BuildSuggestion(
                        searchText,
                        context,
                        caseDefinition));
            }

            suggestions = suggestions
                .OrderByDescending(item => item.Score)
                .ThenBy(item => item.CaseName)
                .Take(5)
                .ToList();

            return new MailAnalysisResult
            {
                Summary = BuildSummary(context),
                Candidates = suggestions
            };
        }

        public IDictionary<string, string> ExtractParameters(
            MailContext context,
            CaseDefinition caseDefinition)
        {
            if (context == null)
            {
                throw new ArgumentNullException(
                    nameof(context));
            }

            if (caseDefinition == null)
            {
                throw new ArgumentNullException(
                    nameof(caseDefinition));
            }

            Dictionary<string, string> values =
                new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase);

            string searchText =
                BuildSearchText(context);

            IList<string> vins =
                FindVins(context);

            foreach (CaseFieldDefinition field
                     in caseDefinition.Fields)
            {
                if (field == null ||
                    string.IsNullOrWhiteSpace(field.Key))
                {
                    continue;
                }

                string key = field.Key.Trim();

                if (string.Equals(
                        key,
                        "vin",
                        StringComparison.OrdinalIgnoreCase))
                {
                    if (vins.Count > 0)
                    {
                        values[key] =
                            string.Join(",", vins);
                    }

                    continue;
                }

                if (string.Equals(
                        key,
                        "site",
                        StringComparison.OrdinalIgnoreCase))
                {
                    string site = FindSite(searchText);

                    if (string.IsNullOrWhiteSpace(site) &&
                        vins.Count > 0)
                    {
                        site = FindSiteFromVin(vins[0]);
                    }

                    if (!string.IsNullOrWhiteSpace(site))
                    {
                        values[key] = site;
                    }
                }
            }

            return values;
        }

        private CaseSuggestion BuildSuggestion(
            string searchText,
            MailContext context,
            CaseDefinition caseDefinition)
        {
            double score = 0.05D;
            List<string> reasons =
                new List<string>();

            IEnumerable<string> tokens =
                GetCaseTokens(caseDefinition);

            foreach (string token in tokens)
            {
                if (searchText.IndexOf(
                        token,
                        StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    score += 0.12D;
                    reasons.Add("命中关键词 " + token);
                }
            }

            if (string.Equals(
                    caseDefinition.Code,
                    "ADD_ORDER_FILE",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (ContainsAny(
                        searchText,
                        "ADD ORDER FILE",
                        "ORDER FILE",
                        "订单文件",
                        "加订单文件"))
                {
                    score += 0.35D;
                    reasons.Add("命中 Order File 业务描述");
                }
            }

            bool needsVin =
                caseDefinition.Fields.Any(
                    field =>
                        string.Equals(
                            field.Key,
                            "vin",
                            StringComparison.OrdinalIgnoreCase));

            IList<string> vins =
                FindVins(context);

            if (needsVin && vins.Count > 0)
            {
                score += 0.25D;
                reasons.Add(
                    "识别到 VIN " +
                    string.Join(",", vins));
            }

            score = Math.Min(score, 0.99D);

            return new CaseSuggestion
            {
                CaseCode = caseDefinition.Code,
                CaseName = caseDefinition.Name,
                Score = score,
                Reason = reasons.Count == 0
                    ? "暂未命中明确关键词"
                    : string.Join("；", reasons)
            };
        }

        private static IEnumerable<string> GetCaseTokens(
            CaseDefinition caseDefinition)
        {
            string source =
                string.Join(
                    " ",
                    new[]
                    {
                        caseDefinition.Code ?? string.Empty,
                        caseDefinition.Name ?? string.Empty,
                        caseDefinition.SummaryTemplate ?? string.Empty
                    });

            return source
                .Split(
                    new[]
                    {
                        ' ', '_', '-', '/', '\\', '.', ':'
                    },
                    StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim())
                .Where(item => item.Length >= 3)
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private static string BuildSearchText(
            MailContext context)
        {
            StringBuilder builder =
                new StringBuilder();

            builder.AppendLine(context.Subject ?? string.Empty);
            builder.AppendLine(context.LatestContent ?? string.Empty);
            builder.AppendLine(context.FullBody ?? string.Empty);

            foreach (MailAttachmentInfo attachment
                     in context.Attachments)
            {
                builder.AppendLine(
                    attachment.FileName ?? string.Empty);
            }

            return builder
                .ToString()
                .ToUpperInvariant();
        }

        private static string BuildSummary(
            MailContext context)
        {
            string latest =
                (context.LatestContent ?? string.Empty)
                .Trim();

            if (latest.Length > 180)
            {
                latest = latest.Substring(0, 180) + "...";
            }

            return string.IsNullOrWhiteSpace(latest)
                ? "未读取到可分析的邮件正文。"
                : latest;
        }

        private static IList<string> FindVins(
            MailContext context)
        {
            List<string> result =
                new List<string>();

            IEnumerable<string> priorityTexts =
                new[]
                {
                    context.LatestContent,
                    context.Subject,
                    context.FullBody
                };

            foreach (string text in priorityTexts)
            {
                AddVinMatches(
                    text,
                    LabeledVinRegex,
                    result);
            }

            foreach (string text in priorityTexts)
            {
                AddVinMatches(
                    text,
                    VinTokenRegex,
                    result);
            }

            return result;
        }

        private static void AddVinMatches(
            string text,
            Regex regex,
            ICollection<string> result)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            MatchCollection matches =
                regex.Matches(text);

            foreach (Match match in matches)
            {
                string value =
                    match.Groups.Count > 1
                        ? match.Groups[1].Value
                        : match.Value;

                value =
                    (value ?? string.Empty)
                    .Trim()
                    .ToUpperInvariant();

                bool containsLetter =
                    value.Any(char.IsLetter);

                bool containsDigit =
                    value.Any(char.IsDigit);

                if (!containsLetter ||
                    !containsDigit ||
                    value.Length != 7)
                {
                    continue;
                }

                if (!result.Contains(value))
                {
                    result.Add(value);
                }
            }
        }

        private static string FindSite(
            string text)
        {
            if (ContainsAny(text, "TIE XI", "TIEXI", "铁西"))
            {
                return "Tiexi";
            }

            if (ContainsAny(text, "DA DONG", "DADONG", "大东"))
            {
                return "Dadong";
            }

            if (ContainsAny(text, "LYDIA"))
            {
                return "Lydia";
            }

            return string.Empty;
        }

        private static string FindSiteFromVin(
            string vin)
        {
            if (string.IsNullOrWhiteSpace(vin))
            {
                return string.Empty;
            }

            switch (char.ToUpperInvariant(vin[0]))
            {
                case 'M':
                    return "Tiexi";

                case 'S':
                    return "Dadong";

                case '1':
                    return "Lydia";

                default:
                    return string.Empty;
            }
        }

        private static bool ContainsAny(
            string text,
            params string[] values)
        {
            if (string.IsNullOrWhiteSpace(text) ||
                values == null)
            {
                return false;
            }

            return values.Any(
                value =>
                    !string.IsNullOrWhiteSpace(value) &&
                    text.IndexOf(
                        value,
                        StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }
}
