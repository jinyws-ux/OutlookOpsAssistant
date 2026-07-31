using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace OutlookOpsAssistant
{
    /// <summary>
    /// 根据 Case 配置把不同 API 回参渲染成统一的结果区域。
    /// 原始 JSON 仅作为调试信息，默认折叠。
    /// </summary>
    public sealed class ResultDisplayControl : UserControl
    {
        private readonly Label statusLabel;
        private readonly Label completedAtLabel;
        private readonly TableLayoutPanel fieldsLayout;
        private readonly Button rawResponseButton;
        private readonly TextBox rawResponseTextBox;

        public ResultDisplayControl()
        {
            Dock = DockStyle.Top;
            AutoSize = true;
            Padding = new Padding(0, 4, 0, 4);

            statusLabel =
                new Label
                {
                    Text = "尚未执行",
                    AutoSize = true,
                    Dock = DockStyle.Top,
                    Font = new Font(
                        SystemFonts.MessageBoxFont,
                        FontStyle.Bold),
                    Margin = new Padding(0, 2, 0, 2)
                };

            completedAtLabel =
                new Label
                {
                    Text = string.Empty,
                    AutoSize = true,
                    Dock = DockStyle.Top,
                    Margin = new Padding(0, 0, 0, 6)
                };

            fieldsLayout =
                new TableLayoutPanel
                {
                    Dock = DockStyle.Top,
                    AutoSize = true,
                    ColumnCount = 2,
                    Margin = new Padding(0, 4, 0, 4)
                };

            fieldsLayout.ColumnStyles.Add(
                new ColumnStyle(
                    SizeType.Absolute,
                    90));

            fieldsLayout.ColumnStyles.Add(
                new ColumnStyle(
                    SizeType.Percent,
                    100));

            rawResponseButton =
                new Button
                {
                    Text = "查看原始回参",
                    AutoSize = true,
                    Visible = false,
                    Margin = new Padding(0, 6, 0, 3)
                };

            rawResponseButton.Click +=
                RawResponseButton_Click;

            rawResponseTextBox =
                new TextBox
                {
                    Dock = DockStyle.Top,
                    Height = 180,
                    Multiline = true,
                    ReadOnly = true,
                    WordWrap = false,
                    ScrollBars = ScrollBars.Both,
                    Font = new Font("Consolas", 9F),
                    Visible = false
                };

            TableLayoutPanel layout =
                new TableLayoutPanel
                {
                    Dock = DockStyle.Top,
                    AutoSize = true,
                    ColumnCount = 1
                };

            layout.ColumnStyles.Add(
                new ColumnStyle(
                    SizeType.Percent,
                    100));

            AddRow(layout, statusLabel);
            AddRow(layout, completedAtLabel);
            AddRow(layout, fieldsLayout);
            AddRow(layout, rawResponseButton);
            AddRow(layout, rawResponseTextBox);

            Controls.Add(layout);
        }

        public void ShowWaiting(
            string message)
        {
            statusLabel.Text =
                string.IsNullOrWhiteSpace(message)
                    ? "尚未执行"
                    : message;

            statusLabel.ForeColor =
                SystemColors.ControlText;

            completedAtLabel.Text =
                string.Empty;

            fieldsLayout.Controls.Clear();
            fieldsLayout.RowStyles.Clear();
            fieldsLayout.RowCount = 0;

            rawResponseTextBox.Text =
                string.Empty;
            rawResponseTextBox.Visible = false;
            rawResponseButton.Visible = false;
            rawResponseButton.Text =
                "查看原始回参";
        }

        public void ShowResult(
            CaseDefinition caseDefinition,
            TicketCreateResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(
                    nameof(result));
            }

            statusLabel.Text =
                result.Success
                    ? "执行成功"
                    : "执行失败";

            statusLabel.ForeColor =
                result.Success
                    ? Color.DarkGreen
                    : Color.DarkRed;

            completedAtLabel.Text =
                "完成时间：" +
                (result.CompletedAt == default(DateTime)
                    ? DateTime.Now
                    : result.CompletedAt)
                .ToString("yyyy-MM-dd HH:mm:ss");

            fieldsLayout.SuspendLayout();

            try
            {
                fieldsLayout.Controls.Clear();
                fieldsLayout.RowStyles.Clear();
                fieldsLayout.RowCount = 0;

                IList<CaseResultFieldDefinition>
                    definitions =
                        caseDefinition == null
                            ? null
                            : caseDefinition.ResultDisplay;

                if (definitions == null ||
                    definitions.Count == 0)
                {
                    AddResultRow(
                        "处理说明",
                        result.Message,
                        CaseResultDisplayType.Status,
                        false);
                }
                else
                {
                    foreach (
                        CaseResultFieldDefinition definition
                        in definitions)
                    {
                        object rawValue =
                            ResolvePath(
                                result,
                                definition.Path);

                        AddResultRow(
                            definition.Label,
                            FormatValue(
                                rawValue,
                                definition.DisplayType),
                            definition.DisplayType,
                            definition.Copyable);
                    }
                }
            }
            finally
            {
                fieldsLayout.ResumeLayout(true);
            }

            rawResponseTextBox.Text =
                JsonHelper.ToPrettyJson(result);
            rawResponseTextBox.Visible = false;
            rawResponseButton.Visible = true;
            rawResponseButton.Text =
                "查看原始回参";
        }

        public void ShowError(
            string message)
        {
            ShowWaiting(
                "执行失败");

            statusLabel.ForeColor =
                Color.DarkRed;

            AddResultRow(
                "错误原因",
                message,
                CaseResultDisplayType.MultiLine,
                false);
        }

        private void AddResultRow(
            string label,
            string value,
            CaseResultDisplayType displayType,
            bool copyable)
        {
            int rowIndex =
                fieldsLayout.RowCount;

            fieldsLayout.RowCount++;
            fieldsLayout.RowStyles.Add(
                new RowStyle(
                    SizeType.AutoSize));

            Label labelControl =
                new Label
                {
                    Text =
                        string.IsNullOrWhiteSpace(label)
                            ? "结果"
                            : label,
                    AutoSize = true,
                    Anchor = AnchorStyles.Left,
                    Margin = new Padding(0, 6, 6, 4)
                };

            Control valueControl =
                CreateValueControl(
                    value,
                    displayType,
                    copyable);

            fieldsLayout.Controls.Add(
                labelControl,
                0,
                rowIndex);

            fieldsLayout.Controls.Add(
                valueControl,
                1,
                rowIndex);
        }

        private static Control CreateValueControl(
            string value,
            CaseResultDisplayType displayType,
            bool copyable)
        {
            string safeValue =
                string.IsNullOrWhiteSpace(value)
                    ? "-"
                    : value;

            Control contentControl;

            if (displayType ==
                    CaseResultDisplayType.MultiLine ||
                displayType ==
                    CaseResultDisplayType.List ||
                displayType ==
                    CaseResultDisplayType.Table)
            {
                contentControl =
                    new TextBox
                    {
                        Text = safeValue,
                        ReadOnly = true,
                        Multiline = true,
                        ScrollBars = ScrollBars.Both,
                        WordWrap =
                            displayType !=
                            CaseResultDisplayType.Table,
                        Dock = DockStyle.Top,
                        Height =
                            displayType ==
                            CaseResultDisplayType.Table
                                ? 150
                                : 75
                    };
            }
            else
            {
                contentControl =
                    new Label
                    {
                        Text = safeValue,
                        AutoSize = true,
                        MaximumSize =
                            new Size(250, 0),
                        Margin = new Padding(0, 6, 0, 4),
                        Font =
                            displayType ==
                            CaseResultDisplayType.Status
                                ? new Font(
                                    SystemFonts.MessageBoxFont,
                                    FontStyle.Bold)
                                : SystemFonts.MessageBoxFont
                    };
            }

            if (!copyable)
            {
                contentControl.Dock = DockStyle.Top;
                return contentControl;
            }

            FlowLayoutPanel wrapper =
                new FlowLayoutPanel
                {
                    Dock = DockStyle.Top,
                    AutoSize = true,
                    FlowDirection =
                        FlowDirection.LeftToRight,
                    WrapContents = false,
                    Margin = new Padding(0)
                };

            contentControl.AutoSize = true;
            contentControl.Dock = DockStyle.None;

            Button copyButton =
                new Button
                {
                    Text = "复制",
                    AutoSize = true,
                    Tag = safeValue,
                    Margin = new Padding(6, 2, 0, 2)
                };

            copyButton.Click +=
                CopyButton_Click;

            wrapper.Controls.Add(contentControl);
            wrapper.Controls.Add(copyButton);

            return wrapper;
        }

        private static void CopyButton_Click(
            object sender,
            EventArgs e)
        {
            Button button =
                sender as Button;

            string value =
                button == null
                    ? string.Empty
                    : Convert.ToString(button.Tag);

            if (string.IsNullOrWhiteSpace(value) ||
                value == "-")
            {
                return;
            }

            try
            {
                Clipboard.SetText(value);
            }
            catch
            {
                // 剪贴板可能暂时被其他进程占用。
            }
        }

        private void RawResponseButton_Click(
            object sender,
            EventArgs e)
        {
            rawResponseTextBox.Visible =
                !rawResponseTextBox.Visible;

            rawResponseButton.Text =
                rawResponseTextBox.Visible
                    ? "收起原始回参"
                    : "查看原始回参";
        }

        private static object ResolvePath(
            object source,
            string path)
        {
            if (source == null ||
                string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            object current = source;

            foreach (
                string segment
                in path.Split('.'))
            {
                if (current == null)
                {
                    return null;
                }

                IDictionary<string, object>
                    genericDictionary =
                        current as
                            IDictionary<string, object>;

                if (genericDictionary != null)
                {
                    KeyValuePair<string, object> pair =
                        genericDictionary.FirstOrDefault(
                            item =>
                                string.Equals(
                                    item.Key,
                                    segment,
                                    StringComparison.OrdinalIgnoreCase));

                    current = pair.Key == null
                        ? null
                        : pair.Value;
                    continue;
                }

                IDictionary dictionary =
                    current as IDictionary;

                if (dictionary != null)
                {
                    object matchedKey = null;

                    foreach (object key
                             in dictionary.Keys)
                    {
                        if (string.Equals(
                                Convert.ToString(key),
                                segment,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            matchedKey = key;
                            break;
                        }
                    }

                    current = matchedKey == null
                        ? null
                        : dictionary[matchedKey];
                    continue;
                }

                PropertyInfo property =
                    current.GetType().GetProperty(
                        segment,
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.IgnoreCase);

                if (property == null)
                {
                    return null;
                }

                current =
                    property.GetValue(
                        current,
                        null);
            }

            return current;
        }

        private static string FormatValue(
            object value,
            CaseResultDisplayType displayType)
        {
            if (value == null)
            {
                return string.Empty;
            }

            if (value is string)
            {
                return Convert.ToString(value);
            }

            IEnumerable enumerable =
                value as IEnumerable;

            if (enumerable != null)
            {
                List<string> items =
                    new List<string>();

                foreach (object item
                         in enumerable)
                {
                    items.Add(
                        item == null
                            ? string.Empty
                            : displayType ==
                                CaseResultDisplayType.Table
                                ? JsonHelper.ToPrettyJson(item)
                                : Convert.ToString(item));
                }

                return string.Join(
                    Environment.NewLine,
                    items);
            }

            return Convert.ToString(value);
        }

        private static void AddRow(
            TableLayoutPanel layout,
            Control control)
        {
            int rowIndex =
                layout.RowCount;

            layout.RowCount++;
            layout.RowStyles.Add(
                new RowStyle(
                    SizeType.AutoSize));
            layout.Controls.Add(
                control,
                0,
                rowIndex);
        }
    }
}