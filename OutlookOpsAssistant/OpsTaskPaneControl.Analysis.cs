using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace OutlookOpsAssistant
{
    public partial class OpsTaskPaneControl
    {
        private Panel analysisPanel;
        private ListBox suggestionListBox;
        private TextBox latestContentTextBox;
        private Label analysisStatusLabel;
        private Button reanalyzeButton;

        private MailContext currentMailContext;
        private IAgentService agentService;

        protected override void OnLoad(
            EventArgs e)
        {
            base.OnLoad(e);
            EnsureAnalysisUi();
        }

        /// <summary>
        /// 由 InspectorSession 传入当前邮件上下文，
        /// 并使用 Mock Agent 执行 Case 推荐和参数预填。
        /// </summary>
        public void LoadMailContext(
            MailContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(
                    nameof(context));
            }

            EnsureAnalysisUi();

            currentMailContext = context;

            SetMailInfo(
                context.Subject,
                context.SenderAddress);

            latestContentTextBox.Text =
                context.LatestContent ??
                string.Empty;

            AnalyzeCurrentMail();
        }

        private void EnsureAnalysisUi()
        {
            if (analysisPanel != null)
            {
                return;
            }

            agentService =
                new MockAgentService();

            analysisPanel =
                new Panel
                {
                    Dock = DockStyle.Top,
                    AutoSize = true,
                    Padding = new Padding(
                        0,
                        0,
                        0,
                        8),
                    Margin = new Padding(
                        0,
                        0,
                        0,
                        8)
                };

            Label titleLabel =
                new Label
                {
                    Text = "邮件分析（Mock Agent）",
                    AutoSize = true,
                    Dock = DockStyle.Top,
                    Font = new Font(
                        SystemFonts.MessageBoxFont,
                        FontStyle.Bold),
                    Margin = new Padding(
                        0,
                        0,
                        0,
                        4)
                };

            Label latestTitleLabel =
                new Label
                {
                    Text = "识别出的最新邮件内容",
                    AutoSize = true,
                    Dock = DockStyle.Top,
                    Margin = new Padding(
                        0,
                        6,
                        0,
                        3)
                };

            latestContentTextBox =
                new TextBox
                {
                    Dock = DockStyle.Top,
                    Height = 100,
                    Multiline = true,
                    ReadOnly = true,
                    ScrollBars = ScrollBars.Vertical
                };

            Label suggestionTitleLabel =
                new Label
                {
                    Text = "候选 Case（匹配度排序）",
                    AutoSize = true,
                    Dock = DockStyle.Top,
                    Margin = new Padding(
                        0,
                        8,
                        0,
                        3)
                };

            suggestionListBox =
                new ListBox
                {
                    Dock = DockStyle.Top,
                    Height = 82,
                    IntegralHeight = false
                };

            suggestionListBox.SelectedIndexChanged +=
                SuggestionListBox_SelectedIndexChanged;

            reanalyzeButton =
                new Button
                {
                    Text = "重新模拟分析",
                    AutoSize = true
                };

            reanalyzeButton.Click +=
                ReanalyzeButton_Click;

            analysisStatusLabel =
                new Label
                {
                    Text = "等待读取邮件",
                    AutoSize = true,
                    MaximumSize = new Size(
                        370,
                        0),
                    Margin = new Padding(
                        8,
                        6,
                        0,
                        0)
                };

            FlowLayoutPanel actionPanel =
                new FlowLayoutPanel
                {
                    Dock = DockStyle.Top,
                    AutoSize = true,
                    FlowDirection =
                        FlowDirection.LeftToRight,
                    WrapContents = true,
                    Margin = new Padding(
                        0,
                        6,
                        0,
                        0)
                };

            actionPanel.Controls.Add(
                reanalyzeButton);
            actionPanel.Controls.Add(
                analysisStatusLabel);

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

            AddAnalysisRow(layout, titleLabel);
            AddAnalysisRow(layout, latestTitleLabel);
            AddAnalysisRow(layout, latestContentTextBox);
            AddAnalysisRow(layout, suggestionTitleLabel);
            AddAnalysisRow(layout, suggestionListBox);
            AddAnalysisRow(layout, actionPanel);

            analysisPanel.Controls.Add(layout);

            Controls.Add(analysisPanel);
            Controls.SetChildIndex(
                analysisPanel,
                0);
        }

        private static void AddAnalysisRow(
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

        private void AnalyzeCurrentMail()
        {
            suggestionListBox.Items.Clear();

            if (currentMailContext == null)
            {
                analysisStatusLabel.Text =
                    "尚未读取当前邮件";
                return;
            }

            reanalyzeButton.Enabled = false;
            analysisStatusLabel.Text =
                "正在使用 Mock Agent 分析...";

            try
            {
                MailAnalysisResult result =
                    agentService.AnalyzeMail(
                        currentMailContext,
                        caseDefinitions);

                foreach (CaseSuggestion suggestion
                         in result.Candidates)
                {
                    suggestionListBox.Items.Add(
                        suggestion);
                }

                if (suggestionListBox.Items.Count > 0)
                {
                    suggestionListBox.SelectedIndex = 0;
                    analysisStatusLabel.Text =
                        "分析完成，请确认候选 Case";
                }
                else
                {
                    analysisStatusLabel.Text =
                        "没有可用的 Case 配置";
                }
            }
            catch (Exception ex)
            {
                analysisStatusLabel.Text =
                    "分析失败：" + ex.Message;
            }
            finally
            {
                reanalyzeButton.Enabled = true;
            }
        }

        private void SuggestionListBox_SelectedIndexChanged(
            object sender,
            EventArgs e)
        {
            CaseSuggestion suggestion =
                suggestionListBox.SelectedItem
                    as CaseSuggestion;

            if (suggestion == null)
            {
                return;
            }

            CaseDefinition selectedCase =
                caseDefinitions.FirstOrDefault(
                    item =>
                        string.Equals(
                            item.Code,
                            suggestion.CaseCode,
                            StringComparison.OrdinalIgnoreCase));

            if (selectedCase == null)
            {
                return;
            }

            caseComboBox.SelectedItem =
                selectedCase;

            ApplyExtractedParameters(
                suggestion.ExtractedParameters);

            resultLabel.Text =
                "Mock Agent：" +
                suggestion.Reason;
        }

        private void ApplyExtractedParameters(
            IDictionary<string, string> values)
        {
            if (values == null)
            {
                return;
            }

            foreach (KeyValuePair<string, string> pair
                     in values)
            {
                Control control;

                if (!fieldControls.TryGetValue(
                        pair.Key,
                        out control))
                {
                    continue;
                }

                TextBox textBox =
                    control as TextBox;

                if (textBox != null)
                {
                    textBox.Text =
                        pair.Value ?? string.Empty;
                    continue;
                }

                ComboBox comboBox =
                    control as ComboBox;

                if (comboBox != null &&
                    comboBox.Items.Contains(pair.Value))
                {
                    comboBox.SelectedItem = pair.Value;
                }
            }
        }

        private void ReanalyzeButton_Click(
            object sender,
            EventArgs e)
        {
            AnalyzeCurrentMail();
        }
    }
}
