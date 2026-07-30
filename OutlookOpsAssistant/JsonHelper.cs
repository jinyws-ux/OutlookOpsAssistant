using System.Text;
using System.Web.Script.Serialization;

namespace OutlookOpsAssistant
{
    public static class JsonHelper
    {
        public static string ToPrettyJson(
            object value)
        {
            JavaScriptSerializer serializer =
                new JavaScriptSerializer
                {
                    MaxJsonLength = int.MaxValue,
                    RecursionLimit = 100
                };

            string json =
                serializer.Serialize(value);

            return FormatJson(json);
        }

        private static string FormatJson(
            string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return string.Empty;
            }

            StringBuilder result =
                new StringBuilder();

            int indentLevel = 0;
            bool inString = false;
            bool escaped = false;

            for (int index = 0;
                 index < json.Length;
                 index++)
            {
                char current = json[index];

                if (inString)
                {
                    result.Append(current);

                    if (escaped)
                    {
                        escaped = false;
                    }
                    else if (current == '\\')
                    {
                        escaped = true;
                    }
                    else if (current == '"')
                    {
                        inString = false;
                    }

                    continue;
                }

                switch (current)
                {
                    case '"':
                        inString = true;
                        result.Append(current);
                        break;

                    case '{':
                    case '[':
                        result.Append(current);
                        result.AppendLine();

                        indentLevel++;
                        AppendIndent(
                            result,
                            indentLevel);
                        break;

                    case '}':
                    case ']':
                        result.AppendLine();

                        indentLevel--;

                        if (indentLevel < 0)
                        {
                            indentLevel = 0;
                        }

                        AppendIndent(
                            result,
                            indentLevel);

                        result.Append(current);
                        break;

                    case ',':
                        result.Append(current);
                        result.AppendLine();

                        AppendIndent(
                            result,
                            indentLevel);
                        break;

                    case ':':
                        result.Append(": ");
                        break;

                    default:
                        if (!char.IsWhiteSpace(current))
                        {
                            result.Append(current);
                        }

                        break;
                }
            }

            return result.ToString();
        }

        private static void AppendIndent(
            StringBuilder result,
            int indentLevel)
        {
            result.Append(
                new string(
                    ' ',
                    indentLevel * 2));
        }
    }
}