using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace OutlookOpsAssistant
{
    public partial class OpsTaskPaneControl : UserControl
    {
        private readonly Label subjectValueLabel;
        private readonly Label senderValueLabel;

        private readonly ComboBox caseComboBox;
        private readonly TableLayoutPanel caseFieldsPanel;
        private readonly ResultDisplayControl resultDisplayControl;

        private readonly Button createTicketButton;

        private readonly Dictionary<string, Control>
            fieldControls;

        private readonly IList<CaseDefinition>
            caseDefinitions;

        private readonly ITicketService ticketService;

        private string currentMailSubject;
        private string currentSender;

        public OpsTaskPaneControl()
        {
            InitializeComponent();

            fieldControls =
                new Dictionary<string, Control>();

            caseDefinitions =
                CaseRegistry.GetAll();

            ticketService =
                new MockTicketService();

            Dock = DockStyle.Fill;
            AutoScroll = true;
            Padding = new Padding(12);

            Label titleLabel =
                new Label
                {
                    Text = "运维工单自动化",
                    AutoSize = true,
                    Font = new Font(
                        Font,
                        FontStyle.Bold),
                    Margin = new Padding(
                        0,
                        0,
                        0,
                        10)
                };

            Label subjectTitleLabel =
                CreateSectionLabel(
                    "邮件主题");

            subjectValueLabel =
                CreateValueLabel();

            Label senderTitleLabel =
                CreateSectionLabel(
                    "发件人");

            senderValueLabel =
                CreateValueLabel();

            Label caseTitleLabel =
                CreateSectionLabel(
                    "Case 类型");

            caseComboBox =
                new ComboBox
                {
                    Dock = DockStyle.Top,
                    DropDownStyle =
                        ComboBoxStyle.DropDownList
                };

            caseComboBox.SelectedIndexChanged +=
                CaseComboBox_SelectedIndexChanged;

            Label parameterTitleLabel =
                CreateSectionLabel(
                    "开单参数");

            caseFieldsPanel =
                new TableLayoutPanel
                {
                    Dock = DockStyle.Top,
                    AutoSize = true,
                    ColumnCount = 2,
                    Padding = new Padding(
                        0,
                        4,
                        0,
                        4)
                };

            caseFieldsPanel.ColumnStyles.Add(
                new ColumnStyle(
                    SizeType.Absolute,
                    90));

            caseFieldsPanel.ColumnStyles.Add(
                new ColumnStyle(
                    SizeType.Percent,
                    100));

            createTicketButton =
                new Button
                {
                    Text = "模拟创建工单",
                    AutoSize = true
                };

            createTicketButton.Click +=
                CreateTicketButton_Click;

            FlowLayoutPanel actionButtonPanel =
                new FlowLayoutPanel
                {
                    Dock = DockStyle.Top,
                    AutoSize = true,
                    FlowDirection =
                        FlowDirection.LeftToRight,
                    WrapContents = true,
                    Margin = new Padding(
                        0,
                        8,
                        0,
                        8)
                };

            actionButtonPanel.Controls.Add(
                createTicketButton);

            Label resultTitleLabel =
                CreateSectionLabel(
                    "处理结果");

            resultDisplayControl =
                new ResultDisplayControl();

            TableLayoutPanel mainLayout =
                new TableLayoutPanel
                {
                    Dock = DockStyle.Top,
                    AutoSize = true,
                    ColumnCount = 1
                };

            mainLayout.ColumnStyles.Add(
                new ColumnStyle(
                    SizeType.Percent,
                    100));

            AddRow(mainLayout, titleLabel);
            AddRow(mainLayout, subjectTitleLabel);
            AddRow(mainLayout, subjectValueLabel);
            AddRow(mainLayout, senderTitleLabel);
            AddRow(mainLayout, senderValueLabel);
            AddRow(mainLayout, caseTitleLabel);
            AddRow(mainLayout, caseComboBox);
            AddRow(mainLayout, parameterTitleLabel);
            AddRow(mainLayout, caseFieldsPanel);
            AddRow(mainLayout, actionButtonPanel);
            AddRow(mainLayout, resultTitleLabel);
            AddRow(mainLayout, resultDisplayControl);

            Controls.Add(mainLayout);

            caseComboBox.DataSource =
                caseDefinitions;

            caseComboBox.DisplayMember =
                "Name";

            if (caseDefinitions.Count > 0)
            {
                caseComboBox.SelectedIndex = 0;
                RenderSelectedCase();
            }
        }

        public void SetMailInfo(
            string subject,
            string sender)
        {
            currentMailSubject =
                subject ?? string.Empty;

            currentSender =
                sender ?? string.Empty;

            subjectValueLabel.Text =
                string.IsNullOrWhiteSpace(subject)
                    ? "-"
                    : subject;

            senderValueLabel.Text =
                string.IsNullOrWhiteSpace(sender)
                    ? "-"
                    : sender;
        }

        private static Label CreateSectionLabel(
            string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Font = new Font(
                    SystemFonts.MessageBoxFont,
                    FontStyle.Bold),
                Dock = DockStyle.Top,
                Margin = new Padding(
                    0,
                    10,
                    0,
                    3)
            };
        }

        private static Label CreateValueLabel()
        {
            return new Label
            {
                Text = "-",
                AutoSize = true,
                Dock = DockStyle.Top,
                MaximumSize =
                    new Size(
                        380,
                        0),
                Margin = new Padding(
                    0,
                    0,
                    0,
                    4)
            };
        }

        private static void AddRow(
            TableLayoutPanel panel,
            Control control)
        {
            int rowIndex =
                panel.RowCount;

            panel.RowCount++;
            panel.RowStyles.Add(
                new RowStyle(
                    SizeType.AutoSize));

            panel.Controls.Add(
                control,
                0,
                rowIndex);
        }

        private void CaseComboBox_SelectedIndexChanged(
            object sender,
            EventArgs e)
        {
            RenderSelectedCase();
        }

        private void RenderSelectedCase()
        {
            CaseDefinition selectedCase =
                caseComboBox.SelectedItem
                    as CaseDefinition;

            caseFieldsPanel.SuspendLayout();

            try
            {
                caseFieldsPanel.Controls.Clear();
                caseFieldsPanel.RowStyles.Clear();
                caseFieldsPanel.RowCount = 0;
                fieldControls.Clear();

                if (selectedCase == null)
                {
                    return;
                }

                foreach (
                    CaseFieldDefinition field
                    in selectedCase.Fields)
                {
                    int rowIndex =
                        caseFieldsPanel.RowCount;

                    caseFieldsPanel.RowCount++;
                    caseFieldsPanel.RowStyles.Add(
                        new RowStyle(
                            SizeType.AutoSize));

                    Label fieldLabel =
                        new Label
                        {
                            Text =
                                field.Label +
                                (field.Required
                                    ? " *"
                                    : string.Empty),
                            AutoSize = true,
                            Anchor = AnchorStyles.Left,
                            Margin = new Padding(
                                0,
                                6,
                                5,
                                4)
                        };

                    Control inputControl =
                        CreateFieldControl(field);

                    caseFieldsPanel.Controls.Add(
                        fieldLabel,
                        0,
                        rowIndex);

                    caseFieldsPanel.Controls.Add(
                        inputControl,
                        1,
                        rowIndex);

                    fieldControls[field.Key] =
                        inputControl;
                }

                resultDisplayControl.ShowWaiting(
                    selectedCase.AutomationEnabled
                        ? "等待执行"
                        : "等待创建工单");
            }
            finally
            {
                caseFieldsPanel.ResumeLayout(true);
            }
        }

        private static Control CreateFieldControl(
            CaseFieldDefinition field)
        {
            if (field.FieldType ==
                CaseFieldType.Select)
            {
                ComboBox comboBox =
                    new ComboBox
                    {
                        Dock = DockStyle.Fill,
                        DropDownStyle =
                            ComboBoxStyle.DropDownList,
                        Margin = new Padding(
                            0,
                            3,
                            0,
                            3)
                    };

                foreach (
                    string option
                    in field.Options)
                {
                    comboBox.Items.Add(option);
                }

                if (!string.IsNullOrWhiteSpace(
                        field.DefaultValue) &&
                    comboBox.Items.Contains(
                        field.DefaultValue))
                {
                    comboBox.SelectedItem =
                        field.DefaultValue;
                }
                else if (
                    comboBox.Items.Count > 0)
                {
                    comboBox.SelectedIndex = 0;
                }

                return comboBox;
            }

            TextBox textBox =
                new TextBox
                {
                    Dock = DockStyle.Fill,
                    Text =
                        field.DefaultValue ??
                        string.Empty,
                    Margin = new Padding(
                        0,
                        3,
                        0,
                        3)
                };

            if (field.FieldType ==
                CaseFieldType.MultiLineText)
            {
                textBox.Multiline = true;
                textBox.Height = 70;
                textBox.ScrollBars =
                    ScrollBars.Vertical;
            }

            return textBox;
        }

        private void CreateTicketButton_Click(
            object sender,
            EventArgs e)
        {
            createTicketButton.Enabled = false;
            resultDisplayControl.ShowWaiting(
                "正在创建工单...");

            try
            {
                TicketCreateRequest request =
                    BuildRequest();

                TicketCreateResult result =
                    ticketService.CreateTicket(
                        request);

                CaseDefinition selectedCase =
                    caseComboBox.SelectedItem
                        as CaseDefinition;

                resultDisplayControl.ShowResult(
                    selectedCase,
                    result);
            }
            catch (Exception ex)
            {
                resultDisplayControl.ShowError(
                    ex.Message);
            }
            finally
            {
                createTicketButton.Enabled = true;
            }
        }

        private TicketCreateRequest BuildRequest()
        {
            CaseDefinition selectedCase =
                caseComboBox.SelectedItem
                    as CaseDefinition;

            if (selectedCase == null)
            {
                throw new InvalidOperationException(
                    "请选择一个 Case。");
            }

            Dictionary<string, string>
                parameterValues =
                    new Dictionary<
                        string,
                        string>();

            foreach (
                CaseFieldDefinition field
                in selectedCase.Fields)
            {
                Control control;

                if (!fieldControls.TryGetValue(
                        field.Key,
                        out control))
                {
                    continue;
                }

                string value =
                    GetControlValue(control);

                if (field.Required &&
                    string.IsNullOrWhiteSpace(value))
                {
                    throw new InvalidOperationException(
                        "请填写必填字段：" +
                        field.Label);
                }

                parameterValues[field.Key] =
                    value;
            }

            string description =
                currentMailContext == null
                    ? string.Empty
                    : currentMailContext.LatestContent ??
                      string.Empty;

            return new TicketCreateRequest
            {
                CaseCode = selectedCase.Code,
                CaseName = selectedCase.Name,
                MailSubject = currentMailSubject,
                Sender = currentSender,
                TicketSummary =
                    selectedCase.SummaryTemplate,
                Description = description,
                AutomationEnabled =
                    selectedCase.AutomationEnabled,
                Parameters = parameterValues
            };
        }

        private static string GetControlValue(
            Control control)
        {
            TextBox textBox =
                control as TextBox;

            if (textBox != null)
            {
                return (
                    textBox.Text ??
                    string.Empty)
                    .Trim();
            }

            ComboBox comboBox =
                control as ComboBox;

            if (comboBox != null)
            {
                return (
                    Convert.ToString(
                        comboBox.SelectedItem) ??
                    string.Empty)
                    .Trim();
            }

            return (
                control.Text ??
                string.Empty)
                .Trim();
        }
    }
}