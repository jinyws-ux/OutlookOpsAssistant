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
        private Label analysisTitleLabel;
        private ListBox suggestionListBox;
        private Label analysisStatusLabel;
        private Button reanalyzeButton;
        private Button enterSelectedCaseButton;
        private Button reloadConfigurationButton;
        private Button configurationDiagnosticsButton;

        private TableLayoutPanel mainWorkflowLayout;
        private int caseTemplateStartRow = -1;
        private int caseTemplateEndRow = -1;

        private MailContext currentMailContext;
        private IAgentService agentService;

        protected override void OnLoad(
            EventArgs e)
        {
            base.OnLoad(e);
            EnsureAnalysisUi();
        }

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

            AnalyzeCurrentMail();
        }

        private void EnsureAnalysisUi()
        {
            if (analysisPanel != null)
            {
                return;
            }

            agentService = CreateConfiguredAgentService();

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

            analysisTitleLabel =
                new Label
                {
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

            UpdateAnalysisTitle();

            Label suggestionTitleLabel =
                new Label
                {
                    Text = "请选择最符合的 Case",
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
                    Height = 105,
                    IntegralHeight = false
                };

            suggestionListBox.SelectedIndexChanged +=
                SuggestionListBox_SelectedIndexChanged;

            suggestionListBox.DoubleClick +=
                SuggestionListBox_DoubleClick;

            enterSelectedCaseButton =
                new Button
                {
                    Text = "进入所选 Case",
                    AutoSize = true,
                    Enabled = false
                };

            enterSelectedCaseButton.Click +=
                EnterSelectedCaseButton_Click;

            reanalyzeButton =
                new Button
                {
                    Text = "重新分析",
                    AutoSize = true
                };

            reanalyzeButton.Click +=
                ReanalyzeButton_Click;

            reloadConfigurationButton =
                new Button
                {
                    Text = "重新加载配置",
                    AutoSize = true
                };

            reloadConfigurationButton.Click +=
                ReloadConfigurationButton_Click;

            configurationDiagnosticsButton =
                new Button
                {
                    Text = "配置诊断",
                    AutoSize = true
                };

            configurationDiagnosticsButton.Click +=
                ConfigurationDiagnosticsButton_Click;

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
                enterSelectedCaseButton);
            actionPanel.Controls.Add(
                reanalyzeButton);
            actionPanel.Controls.Add(
                reloadConfigurationButton);
            actionPanel.Controls.Add(
                configurationDiagnosticsButton);
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

            AddAnalysisRow(layout, analysisTitleLabel);
            AddAnalysisRow(layout, suggestionTitleLabel);
            AddAnalysisRow(layout, suggestionListBox);
            AddAnalysisRow(layout, actionPanel);

            analysisPanel.Controls.Add(layout);

            Controls.Add(analysisPanel);
            Controls.SetChildIndex(
                analysisPanel,
                0);

            ConfigureWorkflowRows();
        }

        private IAgentService CreateConfiguredAgentService()
        {
            string mode =
                runtimeConfiguration == null ||
                runtimeConfiguration.Agent == null
                    ? "mock"
                    : runtimeConfiguration.Agent.Mode;

            if (string.Equals(
                    mode,
                    "mock",
                    StringComparison.OrdinalIgnoreCase))
            {
                return new MockAgentService();
            }

            return new UnavailableAgentService(mode);
        }

        private void UpdateAnalysisTitle()
        {
            if (analysisTitleLabel == null)
            {
                return;
            }

            string mode =
                runtimeConfiguration == null ||
                runtimeConfiguration.Agent == null ||
                string.IsNullOrWhiteSpace(
                    runtimeConfiguration.Agent.Mode)
                    ? "mock"
                    : runtimeConfiguration.Agent.Mode;

            analysisTitleLabel.Text =
                "邮件分析（" + mode + "）";
        }

        private void ConfigureWorkflowRows()
        {
            mainWorkflowLayout =
                caseComboBox.Parent as TableLayoutPanel;

            if (mainWorkflowLayout == null)
            {
                return;
            }

            int caseComboRow =
                mainWorkflowLayout.GetRow(
                    caseComboBox);

            caseTemplateStartRow =
                Math.Max(0, caseComboRow - 1);

            caseTemplateEndRow =
                mainWorkflowLayout.GetRow(
                    resultDisplayControl);

            SetCaseTemplateVisible(false);
        }

        private void SetCaseTemplateVisible(
            bool visible)
        {
            if (mainWorkflowLayout == null ||
                caseTemplateStartRow < 0 ||
                caseTemplateEndRow <
                    caseTemplateStartRow)
            {
                return;
            }

            foreach (Control control
                     in mainWorkflowLayout.Controls)
            {
                int row =
                    mainWorkflowLayout.GetRow(control);

                if (row >= caseTemplateStartRow &&
                    row <= caseTemplateEndRow)
                {
                    control.Visible = visible;
                }
            }
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
            suggestionListBox.SelectedIndex = -1;
            enterSelectedCaseButton.Enabled = false;
            SetCaseTemplateVisible(false);

            if (currentMailContext == null)
            {
                analysisStatusLabel.Text =
                    "尚未读取当前邮件";
                return;
            }

            reanalyzeButton.Enabled = false;
            analysisStatusLabel.Text =
                "正在分析当前邮件...";

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

                analysisStatusLabel.Text =
                    suggestionListBox.Items.Count > 0
                        ? "分析完成，请选择一个候选 Case"
                        : "没有可用的 Case 配置";
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

            enterSelectedCaseButton.Enabled =
                suggestion != null;

            analysisStatusLabel.Text =
                suggestion == null
                    ? "请选择一个候选 Case"
                    : suggestion.Reason;
        }

        private void SuggestionListBox_DoubleClick(
            object sender,
            EventArgs e)
        {
            EnterSelectedCase();
        }

        private void EnterSelectedCaseButton_Click(
            object sender,
            EventArgs e)
        {
            EnterSelectedCase();
        }

        private void EnterSelectedCase()
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
                analysisStatusLabel.Text =
                    "未找到对应的 Case 配置";
                return;
            }

            enterSelectedCaseButton.Enabled = false;
            analysisStatusLabel.Text =
                "正在提取 Case 参数...";

            try
            {
                caseComboBox.SelectedItem =
                    selectedCase;

                RenderSelectedCase();

                IDictionary<string, string> extracted =
                    agentService.ExtractParameters(
                        currentMailContext,
                        selectedCase);

                ApplyExtractedParameters(extracted);
                SetCaseTemplateVisible(true);

                analysisStatusLabel.Text =
                    "已进入 " +
                    selectedCase.Name +
                    "，请确认自动填充内容";

                ScrollControlIntoView(
                    caseComboBox);
            }
            catch (Exception ex)
            {
                analysisStatusLabel.Text =
                    "参数提取失败：" + ex.Message;
            }
            finally
            {
                enterSelectedCaseButton.Enabled = true;
            }
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

        private void ReloadConfigurationButton_Click(
            object sender,
            EventArgs e)
        {
            reloadConfigurationButton.Enabled = false;

            try
            {
                runtimeConfiguration =
                    configurationService.Load();

                caseDefinitions =
                    runtimeConfiguration.Cases;

                BindCaseDefinitions();

                ticketService =
                    CreateConfiguredTicketService();
                UpdateCreateTicketButtonText();

                agentService =
                    CreateConfiguredAgentService();
                UpdateAnalysisTitle();
                SetCaseTemplateVisible(false);

                analysisStatusLabel.Text =
                    "配置已重新加载，共 " +
                    caseDefinitions.Count +
                    " 个 Case";

                if (currentMailContext != null)
                {
                    AnalyzeCurrentMail();
                }
            }
            catch (Exception ex)
            {
                analysisStatusLabel.Text =
                    "配置重载失败：" + ex.Message;
            }
            finally
            {
                reloadConfigurationButton.Enabled = true;
            }
        }

        private void ConfigurationDiagnosticsButton_Click(
            object sender,
            EventArgs e)
        {
            string message =
                runtimeConfiguration == null
                    ? "尚未加载运行配置。"
                    : runtimeConfiguration.BuildDiagnosticText();

            MessageBox.Show(
                message,
                "Outlook 运维助手 - 配置诊断",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }
}
