# 第一阶段开发记录

## 目标

先不连接真实 AI、Dify、RAGFlow 和生产 API，跑通以下主流程：

1. 从当前 Outlook 邮件读取完整上下文。
2. 尝试拆分最新一轮邮件内容。
3. 使用 Mock Agent 对已配置 Case 进行匹配度排序。
4. 用户先选择候选 Case。
5. 进入选定 Case 后，再执行参数提取和自动填充。
6. 继续使用现有模拟开单流程。

## 本次结构调整

- `MailContext.cs`：与 Outlook COM 解耦的邮件上下文模型。
- `OutlookMailContextReader.cs`：读取邮件主题、发件人、正文、最新内容和附件元数据。
- `AgentModels.cs`：Agent 接口、候选 Case 和分析结果模型。
- `MockAgentService.cs`：本地关键词分类和 VIN、厂区提取。
- `OpsTaskPaneControl.Analysis.cs`：邮件分析、候选 Case 展示和字段预填界面。
- `InspectorSession.cs`：完整邮件分析入口。
- `OpsRibbon`：主按钮由“读取选中内容”调整为“分析当前邮件”。

## 第一轮测试反馈修正

状态：已提交，等待本地编译与 Outlook 运行验证。

- “选择的关键内容”不再显示，也不再作为插件主流程的一部分。
- 分析完成后不自动进入排名第一的 Case。
- 用户必须先从候选列表选择 Case，再点击“进入所选 Case”。
- 进入 Case 后才调用参数提取，并重新生成表单，防止带入上一封邮件的数据。
- VIN 不再匹配任意 7 位字母数字组合。
- Mock 阶段优先识别以 `M`、`S`、`1` 开头的 7 位短 VIN，并支持多个 VIN 以英文逗号连接。
- VIN 提取按最新邮件内容、主题、完整邮件链逐级回退；当前层级识别到 VIN 后，不再混入更早邮件中的 VIN。
- 未明确写厂区时，可根据 VIN 首位推断 Tiexi、Dadong 或 Lydia。

## 设计原则

- Outlook 插件负责读取邮件、显示结果、参数确认和流程串联。
- AI/Agent 通过 `IAgentService` 接口隔离，后续替换 Mock 实现时不改 Outlook 主流程。
- Case 分类和 Case 参数提取是两个独立步骤。
- 复杂业务执行继续外置为 API。

## 当前限制

- “最新邮件内容”仅使用基础分隔符规则，复杂邮件格式后续需要继续完善。
- Mock Agent 的匹配度只是开发阶段排序值，不代表真实概率。
- 当前仅有 `ADD_ORDER_FILE` 一个正式 Case 配置，因此候选列表暂时只有一项；增加 Case 配置后会自动显示多个候选项。
- VIN 前缀规则目前仅用于 Mock 测试，后续应迁移到 NAS Case 配置或 Agent 规则中，避免写死在插件核心。
- 尚未在真实 Windows + Outlook + Visual Studio 环境完成编译和运行验证。
