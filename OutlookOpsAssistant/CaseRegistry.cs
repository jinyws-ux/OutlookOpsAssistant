using System.Collections.Generic;

namespace OutlookOpsAssistant
{
    /// <summary>
    /// 仅在外部 Case 配置和本地缓存都不可用时启用。
    /// 正常运行时 Case 应从外部 JSON 加载。
    /// </summary>
    public static class CaseRegistry
    {
        private static readonly List<CaseDefinition> Cases =
            new List<CaseDefinition>
            {
                new CaseDefinition
                {
                    Enabled = true,
                    Code = "ADD_ORDER_FILE",
                    Name = "ADD ORDER FILE",
                    Description = "为指定 VIN 添加 Order File。",
                    SummaryTemplate = "Tiexi_APDM_Add_Order_File",
                    AutomationEnabled = true,
                    MatchKeywords = new List<string>
                    {
                        "ADD ORDER FILE",
                        "ORDER FILE",
                        "订单文件",
                        "加订单文件"
                    },
                    Fields = new List<CaseFieldDefinition>
                    {
                        new CaseFieldDefinition
                        {
                            Key = "site",
                            Label = "厂区",
                            FieldType = CaseFieldType.Select,
                            Required = true,
                            DefaultValue = "Tiexi",
                            Options = new List<string>
                            {
                                "Tiexi",
                                "Dadong",
                                "Lydia"
                            }
                        },
                        new CaseFieldDefinition
                        {
                            Key = "vin",
                            Label = "VIN",
                            FieldType = CaseFieldType.Text,
                            Required = true,
                            DefaultValue = string.Empty
                        },
                        new CaseFieldDefinition
                        {
                            Key = "remark",
                            Label = "补充说明",
                            FieldType = CaseFieldType.MultiLineText,
                            Required = false,
                            DefaultValue = string.Empty
                        }
                    },
                    ResultDisplay = new List<CaseResultFieldDefinition>
                    {
                        new CaseResultFieldDefinition
                        {
                            Label = "工单号",
                            Path = "TicketId",
                            DisplayType = CaseResultDisplayType.Text,
                            Copyable = true
                        },
                        new CaseResultFieldDefinition
                        {
                            Label = "VIN",
                            Path = "Data.vin",
                            DisplayType = CaseResultDisplayType.Text,
                            Copyable = true
                        },
                        new CaseResultFieldDefinition
                        {
                            Label = "厂区",
                            Path = "Data.site",
                            DisplayType = CaseResultDisplayType.Text,
                            Copyable = false
                        },
                        new CaseResultFieldDefinition
                        {
                            Label = "处理说明",
                            Path = "Message",
                            DisplayType = CaseResultDisplayType.Status,
                            Copyable = false
                        }
                    },
                    Api = new CaseApiDefinition
                    {
                        Enabled = false,
                        Method = "POST",
                        TimeoutSeconds = 30
                    }
                }
            };

        public static IList<CaseDefinition> GetAll()
        {
            return Cases.AsReadOnly();
        }
    }
}
