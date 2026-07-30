using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace OutlookOpsAssistant
{
    public partial class OpsTaskPaneControl : UserControl
    {
        private readonly Label subjectValueLabel;
        private readonly Label senderValueLabel;

        private readonly ListBox fragmentListBox;

        private readonly ComboBox caseComboBox;
        private readonly TableLayoutPanel caseFieldsPanel;

        private readonly TextBox previewTextBox;
        private readonly Label resultLabel;

        private readonly Button previewButton;
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

            // 当前使用模拟服务。
            // 后续接真实接口时替换成 HttpTicketService。
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

            Label fragmentTitleLabel =
                CreateSectionLabel(
                    "已选择的关键内容");

            fragmentListBox =
                new ListBox
                {
                    Dock = DockStyle.Top,
                    Height = 110,
                    HorizontalScrollbar = true
                };

            Button deleteFragmentButton =
                new Button
                {
                    Text = "删除选中项",
                    AutoSize = true
                };

            deleteFragmentButton.Click +=
                DeleteFragmentButton_Click;

            Button clearFragmentsButton =
                new Button
                {
                    Text = "清空",
                    AutoSize = true
                };

            clearFragmentsButton.Click +=
                ClearFragmentsButton_Click;

            FlowLayoutPanel fragmentButtonPanel =
                new FlowLayoutPanel
                {
                    Dock = DockStyle.Top,
                    AutoSize = true,
                    FlowDirection =
                        FlowDirection.LeftToRight,
                    WrapContents = false
                };

            fragmentButtonPanel.Controls.Add(
                deleteFragmentButton);

            fragmentButtonPanel.Controls.Add(
                clearFragmentsButton);

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

            previewButton =
                new Button
                {
                    Text = "预览 JSON",
                    AutoSize = true
                };

            previewButton.Click +=
                PreviewButton_Click;

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
                previewButton);

            actionButtonPanel.Controls.Add(
                createTicketButton);

            resultLabel =
                new Label
                {
                    Text = "尚未创建工单",
                    AutoSize = true,
                    Dock = DockStyle.Top,
                    Padding = new Padding(
                        0,
                        4,
                        0,
                        8)
                };

            Label previewTitleLabel =
                CreateSectionLabel(
                    "请求与返回结果");

            previewTextBox =
                new TextBox
                {
                    Dock = DockStyle.Top,
                    Height = 230,
                    Multiline = true,
                    ScrollBars =
                        ScrollBars.Both,
                    ReadOnly = true,
                    WordWrap = false,
                    Font = new Font(
                        "Consolas",
                        9F)
                };

            TableLayoutPanel mainLayout =
                new TableLayoutPanel
                {
                    Dock = DockStyle.Top,
                    AutoSize = true,
                    ColumnCount = 1,
                    RowCount = 16
                };

            mainLayout.ColumnStyles.Add(
                new ColumnStyle(
                    SizeType.Percent,
                    100));

            AddRow(
                mainLayout,
                titleLabel);

            AddRow(
                mainLayout,
                subjectTitleLabel);

            AddRow(
                mainLayout,
                subjectValueLabel);

            AddRow(
                mainLayout,
                senderTitleLabel);

            AddRow(
                mainLayout,
                senderValueLabel);

            AddRow(
                mainLayout,
                fragmentTitleLabel);

            AddRow(
                mainLayout,
                fragmentListBox);

            AddRow(
                mainLayout,
                fragmentButtonPanel);

            AddRow(
                mainLayout,
                caseTitleLabel);

            AddRow(
                mainLayout,
                caseComboBox);

            AddRow(
                mainLayout,
                parameterTitleLabel);

            AddRow(
                mainLayout,
                caseFieldsPanel);

            AddRow(
                mainLayout,
                actionButtonPanel);

            AddRow(
                mainLayout,
                resultLabel);

            AddRow(
                mainLayout,
                previewTitleLabel);

            AddRow(
                mainLayout,
                previewTextBox);

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

        public bool AddFragment(
            string fragment)
        {
            string normalized =
                (fragment ?? string.Empty)
                .Trim();

            if (string.IsNullOrWhiteSpace(
                    normalized))
            {
                return false;
            }

            bool alreadyExists =
                fragmentListBox.Items
                    .Cast<object>()
                    .Any(item =>
                        string.Equals(
                            Convert.ToString(item),
                            normalized,
                            StringComparison.Ordinal));

            if (alreadyExists)
            {
                return false;
            }

            fragmentListBox.Items.Add(
                normalized);

            fragmentListBox.SelectedIndex =
                fragmentListBox.Items.Count - 1;

            return true;
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
                        360,
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
                panel.Controls.Count;

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
                fieldControls.Clear();

                if (selectedCase == null)
                {
                    return;
                }

                int rowIndex = 0;

                foreach (
                    CaseFieldDefinition field
                    in selectedCase.Fields)
                {
                    Label fieldLabel =
                        new Label
                        {
                            Text =
                                field.Label +
                                (field.Required
                                    ? " *"
                                    : string.Empty),
                            AutoSize = true,
                            Anchor =
                                AnchorStyles.Left,
                            Margin = new Padding(
                                0,
                                6,
                                5,
                                4)
                        };

                    Control inputControl =
                        CreateFieldControl(field);

                    caseFieldsPanel.RowStyles.Add(
                        new RowStyle(
                            SizeType.AutoSize));

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

                    rowIndex++;
                }

                caseFieldsPanel.RowCount =
                    rowIndex;

                resultLabel.Text =
                    selectedCase.AutomationEnabled
                        ? "该 Case 支持自动化处理"
                        : "该 Case 仅支持开单";
            }
            finally
            {
                caseFieldsPanel.ResumeLayout(
                    true);
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

        private void PreviewButton_Click(
            object sender,
            EventArgs e)
        {
            try
            {
                TicketCreateRequest request =
                    BuildRequest();

                previewTextBox.Text =
                    JsonHelper.ToPrettyJson(
                        request);

                resultLabel.Text =
                    "参数校验通过，尚未创建工单";
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "参数检查失败",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void CreateTicketButton_Click(
            object sender,
            EventArgs e)
        {
            createTicketButton.Enabled =
                false;

            try
            {
                TicketCreateRequest request =
                    BuildRequest();

                TicketCreateResult result =
                    ticketService.CreateTicket(
                        request);

                previewTextBox.Text =
                    JsonHelper.ToPrettyJson(
                        new
                        {
                            Request = request,
                            Response = result
                        });

                if (result.Success)
                {
                    resultLabel.Text =
                        "模拟工单号：" +
                        result.TicketId;

                    MessageBox.Show(
                        result.Message +
                        "\n\n工单号：" +
                        result.TicketId,
                        "创建成功",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    resultLabel.Text =
                        "创建失败：" +
                        result.Message;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "创建工单失败",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                createTicketButton.Enabled =
                    true;
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
                    string.IsNullOrWhiteSpace(
                        value))
                {
                    throw new InvalidOperationException(
                        "请填写必填字段：" +
                        field.Label);
                }

                parameterValues[field.Key] =
                    value;
            }

            List<string> fragments =
                fragmentListBox.Items
                    .Cast<object>()
                    .Select(item =>
                        Convert.ToString(item) ??
                        string.Empty)
                    .Where(item =>
                        !string.IsNullOrWhiteSpace(
                            item))
                    .ToList();

            string description =
                fragments.Count == 0
                    ? string.Empty
                    : string.Join(
                        Environment.NewLine +
                        Environment.NewLine,
                        fragments);

            return new TicketCreateRequest
            {
                CaseCode =
                    selectedCase.Code,

                CaseName =
                    selectedCase.Name,

                MailSubject =
                    currentMailSubject,

                Sender =
                    currentSender,

                TicketSummary =
                    selectedCase.SummaryTemplate,

                Description =
                    description,

                AutomationEnabled =
                    selectedCase.AutomationEnabled,

                Parameters =
                    parameterValues,

                SelectedFragments =
                    fragments
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

        private void DeleteFragmentButton_Click(
            object sender,
            EventArgs e)
        {
            int selectedIndex =
                fragmentListBox.SelectedIndex;

            if (selectedIndex >= 0)
            {
                fragmentListBox.Items.RemoveAt(
                    selectedIndex);
            }
        }

        private void ClearFragmentsButton_Click(
            object sender,
            EventArgs e)
        {
            if (fragmentListBox.Items.Count == 0)
            {
                return;
            }

            DialogResult result =
                MessageBox.Show(
                    "确定要清空全部关键内容吗？",
                    "Outlook 运维助手",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

            if (result ==
                DialogResult.Yes)
            {
                fragmentListBox.Items.Clear();
            }
        }
    }
}