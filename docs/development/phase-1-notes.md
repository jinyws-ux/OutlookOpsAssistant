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
- `ResultDisplayControl.cs`：按 Case 配置渲染不同业务回参。
- `ConfigurationModels.cs`：Bootstrap、环境参数和 Case JSON 模型。
- `ConfigurationService.cs`：配置查找、校验、本地缓存和保底回退。
- `InspectorSession.cs`：完整邮件分析入口。
- `OpsRibbon`：主按钮由“读取选中内容”调整为“分析当前邮件”。

## 第一轮测试反馈修正

- “选择的关键内容”不再显示，也不再作为插件主流程的一部分。
- 分析完成后不自动进入排名第一的 Case。
- 用户必须先从候选列表选择 Case，再点击“进入所选 Case”。
- 进入 Case 后才调用参数提取，并重新生成表单，防止带入上一封邮件的数据。
- VIN 不再匹配任意 7 位字母数字组合。
- Mock 阶段优先识别以 `M`、`S`、`1` 开头的 7 位短 VIN，并支持多个 VIN 以英文逗号连接。
- VIN 提取按最新邮件内容、主题、完整邮件链逐级回退；当前层级识别到 VIN 后，不再混入更早邮件中的 VIN。
- 未明确写厂区时，可根据 VIN 首位推断 Tiexi、Dadong 或 Lydia。

## 第二轮界面反馈修正

- 不再在主界面铺开“识别出的最新邮件内容”。邮件正文仍保留在 `MailContext` 中供 Agent 使用，但不占用操作者界面。
- 删除“预览 JSON”和“请求与返回结果”主区域。
- 主界面只显示处理后的业务结果。
- 所有 Case 共用固定的成功/失败状态与完成时间。
- 每个 Case 通过 `ResultDisplay` 配置决定显示哪些回参字段、字段路径、展示类型以及是否可复制。
- 原始 JSON 仅保留为“查看原始回参”调试入口，默认折叠，并且只显示回参，不显示请求参数。
- 当前支持 `Text`、`Status`、`MultiLine`、`List`、`Table` 五种结果展示类型。

## 第三轮：外部配置层

状态：已提交，等待本地编译与 Outlook 运行验证。

- Case 不再由主流程直接读取写死的 `CaseRegistry`，正常情况下从 `config\cases.json` 加载。
- 新增本地测试用的第二个 Case：`IPS-Q Battery Query`，用于验证多个候选、不同表单和不同结果字段。
- `bootstrap.json` 外置环境名称、Case 地址、Agent 模式与地址、API 开关与基础地址、缓存和日志目录。
- Bootstrap 支持环境变量、ProgramData、LocalAppData 和插件目录四级查找。
- `caseConfigPath` 支持相对路径、绝对路径、UNC 路径和环境变量。
- 主 Case 配置读取成功后自动写入本地缓存；NAS 或主配置暂时不可用时自动读取缓存。
- 主配置和缓存均不可用时，才使用 `CaseRegistry` 中的最小内置保底 Case。
- 任务窗格新增“重新加载配置”和“配置诊断”，修改 JSON 后不需要重新编译。
- 配置成未实现的真实 Agent 模式时会明确报错，不会静默切回 Mock。
- 完整配置说明记录在 `docs/configuration.md`。

## 设计原则

- Outlook 插件负责读取邮件、显示结果、参数确认和流程串联。
- AI/Agent 通过 `IAgentService` 接口隔离，后续替换 Mock 实现时不改 Outlook 读取和界面主流程。
- Case 分类和 Case 参数提取是两个独立步骤。
- 复杂业务执行继续外置为 API。
- 不统一不同 Case 的业务结果内容，只统一可复用的结果展示组件。
- 所有只有进入生产环境后才能确定的地址和开关，原则上不得写死在 DLL 中。

## 当前限制

- “最新邮件内容”仅使用基础分隔符规则，复杂邮件格式后续需要继续完善。
- Mock Agent 的匹配度只是开发阶段排序值，不代表真实概率。
- VIN 前缀规则目前仍用于 Mock 测试，后续应迁移到 Case 配置或 Agent 规则中。
- `Table` 类型当前先以只读结构化文本方式显示，等出现真实表格型 Case 后再优化成专用表格控件。
- 外部配置已经预留真实 Agent 和 API 参数，但 HTTP Agent 与通用 API 调用器尚未开发。
- API Key、密码和生产凭据不得写入普通 JSON，后续使用 Windows 凭据管理器或内部网关。
- 尚未在真实 Windows + Outlook + Visual Studio 环境完成本轮编译和运行验证。
