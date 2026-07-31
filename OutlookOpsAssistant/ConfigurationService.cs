using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Script.Serialization;

namespace OutlookOpsAssistant
{
    public sealed class ConfigurationService
    {
        public const string BootstrapEnvironmentVariable =
            "OUTLOOK_OPS_ASSISTANT_BOOTSTRAP";

        private readonly JavaScriptSerializer serializer;

        public ConfigurationService()
        {
            serializer = new JavaScriptSerializer
            {
                MaxJsonLength = int.MaxValue,
                RecursionLimit = 100
            };
        }

        public RuntimeConfigurationSnapshot Load()
        {
            List<string> warnings = new List<string>();

            string bootstrapPath;
            BootstrapConfiguration bootstrap =
                LoadBootstrap(warnings, out bootstrapPath);

            NormalizeBootstrap(bootstrap);

            string bootstrapDirectory =
                string.IsNullOrWhiteSpace(bootstrapPath)
                    ? GetAssemblyDirectory()
                    : Path.GetDirectoryName(bootstrapPath);

            string caseConfigPath = ResolvePath(
                bootstrap.CaseConfigPath,
                bootstrapDirectory);

            string cacheDirectory = ResolvePath(
                bootstrap.Paths.CacheDirectory,
                bootstrapDirectory);

            if (string.IsNullOrWhiteSpace(cacheDirectory))
            {
                cacheDirectory = Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.LocalApplicationData),
                    "OutlookOpsAssistant",
                    "cache");
            }

            string logDirectory = ResolvePath(
                bootstrap.Paths.LogDirectory,
                bootstrapDirectory);

            if (string.IsNullOrWhiteSpace(logDirectory))
            {
                logDirectory = Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.LocalApplicationData),
                    "OutlookOpsAssistant",
                    "logs");
            }

            TryCreateDirectory(cacheDirectory, "缓存", warnings);
            TryCreateDirectory(logDirectory, "日志", warnings);

            string cacheFilePath = Path.Combine(
                cacheDirectory,
                "cases.cache.json");

            IList<CaseDefinition> cases;
            string caseSource;
            bool usedFallbackCases = false;
            string rawCaseJson;
            string loadError;

            if (TryLoadCases(
                    caseConfigPath,
                    out cases,
                    out rawCaseJson,
                    out loadError))
            {
                caseSource = "主配置";
                SaveCaseCache(
                    cacheFilePath,
                    rawCaseJson,
                    warnings);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(loadError))
                {
                    warnings.Add(
                        "主 Case 配置读取失败：" + loadError);
                }

                string cacheError;
                string ignoredJson;

                if (TryLoadCases(
                        cacheFilePath,
                        out cases,
                        out ignoredJson,
                        out cacheError))
                {
                    caseSource = "本地缓存";
                    warnings.Add(
                        "当前正在使用 Case 本地缓存。" );
                }
                else
                {
                    cases = CaseRegistry
                        .GetAll()
                        .Where(item =>
                            item != null && item.Enabled)
                        .ToList();

                    caseSource = "内置保底配置";
                    usedFallbackCases = true;
                    warnings.Add(
                        "未能读取主配置或缓存，已使用内置保底 Case。" );

                    if (!string.IsNullOrWhiteSpace(cacheError) &&
                        File.Exists(cacheFilePath))
                    {
                        warnings.Add(
                            "本地缓存读取失败：" + cacheError);
                    }
                }
            }

            return new RuntimeConfigurationSnapshot
            {
                EnvironmentName = bootstrap.Environment,
                BootstrapPath = string.IsNullOrWhiteSpace(bootstrapPath)
                    ? "未找到外部 Bootstrap，使用内置默认"
                    : bootstrapPath,
                CaseConfigPath = caseConfigPath,
                CaseConfigSource = caseSource,
                CacheDirectory = cacheDirectory,
                LogDirectory = logDirectory,
                Agent = bootstrap.Agent,
                Api = bootstrap.Api,
                Cases = cases,
                Warnings = warnings,
                UsedFallbackCases = usedFallbackCases,
                LoadedAt = DateTime.Now
            };
        }

        private BootstrapConfiguration LoadBootstrap(
            ICollection<string> warnings,
            out string loadedPath)
        {
            loadedPath = null;

            foreach (string candidate in
                     GetBootstrapCandidates(warnings))
            {
                if (string.IsNullOrWhiteSpace(candidate) ||
                    !File.Exists(candidate))
                {
                    continue;
                }

                try
                {
                    string json = File.ReadAllText(
                        candidate,
                        Encoding.UTF8);

                    BootstrapConfiguration configuration =
                        serializer.Deserialize<BootstrapConfiguration>(
                            json);

                    if (configuration == null)
                    {
                        throw new InvalidDataException(
                            "文件内容为空。" );
                    }

                    loadedPath = candidate;
                    return configuration;
                }
                catch (Exception ex)
                {
                    warnings.Add(
                        "Bootstrap 无法读取：" +
                        candidate +
                        "；" +
                        ex.Message);
                }
            }

            return CreateDefaultBootstrap();
        }

        private static IEnumerable<string> GetBootstrapCandidates(
            ICollection<string> warnings)
        {
            List<string> candidates = new List<string>();

            string environmentPath =
                Environment.GetEnvironmentVariable(
                    BootstrapEnvironmentVariable);

            if (!string.IsNullOrWhiteSpace(environmentPath))
            {
                string resolvedEnvironmentPath = ResolvePath(
                    environmentPath,
                    Environment.CurrentDirectory);

                candidates.Add(resolvedEnvironmentPath);

                if (!File.Exists(resolvedEnvironmentPath))
                {
                    warnings.Add(
                        "环境变量 " +
                        BootstrapEnvironmentVariable +
                        " 指向的文件不存在：" +
                        resolvedEnvironmentPath);
                }
            }

            string programData =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.CommonApplicationData);

            if (!string.IsNullOrWhiteSpace(programData))
            {
                candidates.Add(Path.Combine(
                    programData,
                    "OutlookOpsAssistant",
                    "bootstrap.json"));
            }

            string localAppData =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData);

            if (!string.IsNullOrWhiteSpace(localAppData))
            {
                candidates.Add(Path.Combine(
                    localAppData,
                    "OutlookOpsAssistant",
                    "bootstrap.json"));
            }

            candidates.Add(Path.Combine(
                GetAssemblyDirectory(),
                "config",
                "bootstrap.json"));

            return candidates
                .Where(item =>
                    !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private static BootstrapConfiguration CreateDefaultBootstrap()
        {
            return new BootstrapConfiguration
            {
                Environment = "test",
                CaseConfigPath = Path.Combine(
                    GetAssemblyDirectory(),
                    "config",
                    "cases.json"),
                Agent = new AgentConfiguration
                {
                    Mode = "mock",
                    TimeoutSeconds = 30
                },
                Api = new ApiConfiguration
                {
                    Enabled = false,
                    TimeoutSeconds = 30
                },
                Paths = new RuntimePathConfiguration
                {
                    CacheDirectory = "%LOCALAPPDATA%\\OutlookOpsAssistant\\cache",
                    LogDirectory = "%LOCALAPPDATA%\\OutlookOpsAssistant\\logs"
                }
            };
        }

        private static void NormalizeBootstrap(
            BootstrapConfiguration bootstrap)
        {
            if (bootstrap.Agent == null)
            {
                bootstrap.Agent = new AgentConfiguration();
            }

            if (bootstrap.Api == null)
            {
                bootstrap.Api = new ApiConfiguration();
            }

            if (bootstrap.Paths == null)
            {
                bootstrap.Paths = new RuntimePathConfiguration();
            }

            if (string.IsNullOrWhiteSpace(bootstrap.Environment))
            {
                bootstrap.Environment = "test";
            }

            if (string.IsNullOrWhiteSpace(bootstrap.Agent.Mode))
            {
                bootstrap.Agent.Mode = "mock";
            }

            if (bootstrap.Agent.TimeoutSeconds <= 0)
            {
                bootstrap.Agent.TimeoutSeconds = 30;
            }

            if (bootstrap.Api.TimeoutSeconds <= 0)
            {
                bootstrap.Api.TimeoutSeconds = 30;
            }
        }

        private bool TryLoadCases(
            string path,
            out IList<CaseDefinition> cases,
            out string rawJson,
            out string error)
        {
            cases = null;
            rawJson = null;
            error = null;

            if (string.IsNullOrWhiteSpace(path))
            {
                error = "未配置 CaseConfigPath。";
                return false;
            }

            if (!File.Exists(path))
            {
                error = "文件不存在：" + path;
                return false;
            }

            try
            {
                rawJson = File.ReadAllText(
                    path,
                    Encoding.UTF8);

                CaseConfigurationDocument document =
                    serializer.Deserialize<CaseConfigurationDocument>(
                        rawJson);

                cases = ConvertCases(document);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static IList<CaseDefinition> ConvertCases(
            CaseConfigurationDocument document)
        {
            if (document == null || document.Cases == null)
            {
                throw new InvalidDataException(
                    "Case 配置中缺少 cases。" );
            }

            List<CaseDefinition> result =
                new List<CaseDefinition>();

            HashSet<string> caseCodes =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (CaseConfigurationItem source
                     in document.Cases)
            {
                if (source == null || source.Enabled == false)
                {
                    continue;
                }

                string code =
                    (source.Code ?? string.Empty).Trim();

                string name =
                    (source.Name ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(code))
                {
                    throw new InvalidDataException(
                        "存在未填写 code 的 Case。" );
                }

                if (string.IsNullOrWhiteSpace(name))
                {
                    throw new InvalidDataException(
                        "Case " + code + " 未填写 name。" );
                }

                if (!caseCodes.Add(code))
                {
                    throw new InvalidDataException(
                        "Case code 重复：" + code);
                }

                CaseDefinition target =
                    new CaseDefinition
                    {
                        Enabled = true,
                        Code = code,
                        Name = name,
                        Description = source.Description,
                        SummaryTemplate =
                            source.SummaryTemplate,
                        AutomationEnabled =
                            source.AutomationEnabled,
                        MatchKeywords = NormalizeStrings(
                            source.MatchKeywords),
                        Api = ConvertCaseApi(source.Api)
                    };

                target.Fields = ConvertFields(
                    code,
                    source.Fields);

                target.ResultDisplay = ConvertResultFields(
                    code,
                    source.ResultDisplay);

                result.Add(target);
            }

            if (result.Count == 0)
            {
                throw new InvalidDataException(
                    "Case 配置中没有启用的 Case。" );
            }

            return result.AsReadOnly();
        }

        private static List<CaseFieldDefinition> ConvertFields(
            string caseCode,
            IList<CaseFieldConfiguration> sourceFields)
        {
            List<CaseFieldDefinition> result =
                new List<CaseFieldDefinition>();

            HashSet<string> keys =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (CaseFieldConfiguration source
                     in sourceFields ??
                        new List<CaseFieldConfiguration>())
            {
                if (source == null)
                {
                    continue;
                }

                string key =
                    (source.Key ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(key))
                {
                    throw new InvalidDataException(
                        "Case " + caseCode +
                        " 存在未填写 key 的字段。" );
                }

                if (!keys.Add(key))
                {
                    throw new InvalidDataException(
                        "Case " + caseCode +
                        " 字段 key 重复：" + key);
                }

                result.Add(new CaseFieldDefinition
                {
                    Key = key,
                    Label = string.IsNullOrWhiteSpace(source.Label)
                        ? key
                        : source.Label.Trim(),
                    FieldType = ParseFieldType(source.Type),
                    Required = source.Required,
                    DefaultValue = source.DefaultValue,
                    Options = NormalizeStrings(source.Options)
                });
            }

            return result;
        }

        private static List<CaseResultFieldDefinition>
            ConvertResultFields(
                string caseCode,
                IList<CaseResultFieldConfiguration> sourceFields)
        {
            List<CaseResultFieldDefinition> result =
                new List<CaseResultFieldDefinition>();

            foreach (CaseResultFieldConfiguration source
                     in sourceFields ??
                        new List<CaseResultFieldConfiguration>())
            {
                if (source == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(source.Path))
                {
                    throw new InvalidDataException(
                        "Case " + caseCode +
                        " 存在未填写 path 的结果字段。" );
                }

                result.Add(new CaseResultFieldDefinition
                {
                    Label = string.IsNullOrWhiteSpace(source.Label)
                        ? source.Path.Trim()
                        : source.Label.Trim(),
                    Path = source.Path.Trim(),
                    DisplayType = ParseResultType(source.Type),
                    Copyable = source.Copyable
                });
            }

            return result;
        }

        private static CaseApiDefinition ConvertCaseApi(
            CaseApiConfiguration source)
        {
            if (source == null)
            {
                return new CaseApiDefinition();
            }

            return new CaseApiDefinition
            {
                Enabled = source.Enabled,
                Endpoint = source.Endpoint,
                Method = string.IsNullOrWhiteSpace(source.Method)
                    ? "POST"
                    : source.Method.Trim().ToUpperInvariant(),
                TimeoutSeconds = source.TimeoutSeconds <= 0
                    ? 30
                    : source.TimeoutSeconds
            };
        }

        private static CaseFieldType ParseFieldType(
            string value)
        {
            switch ((value ?? string.Empty)
                .Trim()
                .ToLowerInvariant())
            {
                case "select":
                case "dropdown":
                    return CaseFieldType.Select;

                case "multiline":
                case "multilinetext":
                case "textarea":
                    return CaseFieldType.MultiLineText;

                default:
                    return CaseFieldType.Text;
            }
        }

        private static CaseResultDisplayType ParseResultType(
            string value)
        {
            switch ((value ?? string.Empty)
                .Trim()
                .ToLowerInvariant())
            {
                case "status":
                    return CaseResultDisplayType.Status;

                case "multiline":
                    return CaseResultDisplayType.MultiLine;

                case "list":
                    return CaseResultDisplayType.List;

                case "table":
                    return CaseResultDisplayType.Table;

                default:
                    return CaseResultDisplayType.Text;
            }
        }

        private static List<string> NormalizeStrings(
            IEnumerable<string> values)
        {
            return (values ?? Enumerable.Empty<string>())
                .Where(item =>
                    !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static void SaveCaseCache(
            string cacheFilePath,
            string json,
            ICollection<string> warnings)
        {
            if (string.IsNullOrWhiteSpace(cacheFilePath) ||
                string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            try
            {
                string directory =
                    Path.GetDirectoryName(cacheFilePath);

                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(
                    cacheFilePath,
                    json,
                    new UTF8Encoding(false));
            }
            catch (Exception ex)
            {
                warnings.Add(
                    "Case 缓存写入失败：" + ex.Message);
            }
        }

        private static void TryCreateDirectory(
            string path,
            string displayName,
            ICollection<string> warnings)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            try
            {
                Directory.CreateDirectory(path);
            }
            catch (Exception ex)
            {
                warnings.Add(
                    displayName +
                    "目录无法创建：" +
                    path +
                    "；" +
                    ex.Message);
            }
        }

        private static string ResolvePath(
            string configuredPath,
            string baseDirectory)
        {
            if (string.IsNullOrWhiteSpace(configuredPath))
            {
                return null;
            }

            string expanded =
                Environment.ExpandEnvironmentVariables(
                    configuredPath.Trim());

            if (!Path.IsPathRooted(expanded))
            {
                expanded = Path.Combine(
                    string.IsNullOrWhiteSpace(baseDirectory)
                        ? Environment.CurrentDirectory
                        : baseDirectory,
                    expanded);
            }

            return Path.GetFullPath(expanded);
        }

        private static string GetAssemblyDirectory()
        {
            string location =
                Assembly.GetExecutingAssembly().Location;

            if (string.IsNullOrWhiteSpace(location))
            {
                return AppDomain.CurrentDomain.BaseDirectory;
            }

            return Path.GetDirectoryName(location);
        }
    }
}
