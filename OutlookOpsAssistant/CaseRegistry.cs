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
                    }
                }
            };

        public static IList<CaseDefinition> GetAll()
        {
            return Cases.AsReadOnly();
        }
    }
}