using System.Collections.Generic;

namespace OutlookOpsAssistant
{
    public static class CaseRegistry
    {
        private static readonly List<CaseDefinition> Cases =
            new List<CaseDefinition>
            {
                new CaseDefinition
                {
                    Code = "ADD_ORDER_FILE",
                    Name = "ADD ORDER FILE",
                    SummaryTemplate = "Tiexi_APDM_Add_Order_File",
                    AutomationEnabled = true,

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
                    }
                }
            };

        public static IList<CaseDefinition> GetAll()
        {
            return Cases.AsReadOnly();
        }
    }
}