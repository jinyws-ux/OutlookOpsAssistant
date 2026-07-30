# 第一阶段开发记录

## 目标

先不连接真实 AI、Dify、RAGFlow 和生产 API，跑通以下主流程：

1. 从当前 Outlook 邮件读取完整上下文。
2. 尝试拆分最新一轮邮件内容。
3. 使用 Mock Agent 对已配置 Case 进行匹配度排序。
4. 用户确认候选 Case。
5. 根据邮件内容自动预填可识别字段。
6. 继续使用现有模拟开单流程。

## 本次结构调整

- `MailContext.cs`：与 Outlook COM 解耦的邮件上下文模型。
- `OutlookMailContextReader.cs`：读取邮件主题、发件人、正文、最新内容和附件元数据。
- `AgentModels.cs`：Agent 接口、候选 Case 和分析结果模型。
- `MockAgentService.cs`：本地关键词分类和 VIN、厂区提取。
- `OpsTaskPaneControl.Analysis.cs`：邮件分析、候选 Case 展示和字段预填界面。
- `InspectorSession.cs`：新增完整邮件分析入口。
- `OpsRibbon`：主按钮由“读取选中内容”调整为“分析当前邮件”。

## 设计原则

- Outlook 插件负责读取邮件、显示结果、参数确认和流程串联。
- AI/Agent 通过 `IAgentService` 接口隔离，后续替换 Mock 实现时不改 Outlook 主流程。
- 复杂业务执行继续外置为 API。
- 原有选中文字能力暂时保留在代码中，但不再作为默认入口。

## 当前限制

- “最新邮件内容”仅使用基础分隔符规则，复杂邮件格式后续需要继续完善。
- Mock Agent 的匹配度只是开发阶段排序值，不代表真实概率。
- 当前仅有 `ADD_ORDER_FILE` 一个正式 Case 配置，因此候选列表暂时可能只有一项。
- 尚未在真实 Windows + Outlook + Visual Studio 环境完成编译和运行验证。
