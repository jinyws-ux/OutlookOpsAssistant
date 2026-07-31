# 第二阶段开发记录：外部服务拆分与邮件导出

## 本阶段目标

在尚未取得生产 Agent、Helix 和 Case API 协议的情况下，先把插件内部职责拆开，并完成可以在测试环境验证的 Outlook 邮件导出能力。

## 已完成结构

```text
IAgentService
├─ MockAgentService
└─ UnavailableAgentService

IHelixService
├─ MockHelixService
└─ UnavailableHelixService

ICaseApiService
├─ MockCaseApiService
└─ UnavailableCaseApiService

IMailExporter
└─ OutlookMsgExporter

ITicketService
└─ WorkflowTicketService
```

## 当前执行流程

```text
动态 Case 表单确认
→ Helix 创建工单
→ 当前 Outlook 邮件导出为 Unicode MSG
→ 将 MSG 上传为工单附件
→ 根据全局与 Case 开关执行 Case API
→ 合并各步骤结果并按 Case 配置展示
```

## 关键设计决定

- Helix 和 Case API 是两套独立服务，避免把业务执行逻辑混入开单接口。
- 邮件导出只负责本地生成 MSG，不了解 Helix 上传协议。
- 邮件附件上传失败不否定已经成功创建的工单。
- Case API 失败会保留工单号，并明确显示“工单已创建，但业务执行失败”。
- 未确认真实协议前，`http` 等非 Mock 模式不会发送猜测结构的生产请求。
- 修改 `bootstrap.json` 并点击“重新加载配置”后，会重新选择 Agent、Helix 和 Case API 实现。

## 新增外部配置

- Agent 回复生成地址 `replyEndpoint`。
- Case API 模式 `api.mode`。
- Helix 模式、基础地址、开单地址和附件地址。
- 是否上传当前邮件。
- MSG 临时导出目录。
- 上传后是否删除临时 MSG。

## 测试配置说明

当前测试配置：

- Agent：Mock。
- Helix：Mock。
- Case API：Mock 且启用。
- 当前邮件附件：启用。
- 导出后删除：关闭，便于检查 MSG 文件。

默认导出目录：

```text
%LOCALAPPDATA%\OutlookOpsAssistant\temp
```

完成验证后应将 `deleteExportedMailAfterUpload` 改为 `true`。

## 本地验证清单

- [ ] Visual Studio 解决方案正常编译。
- [ ] 打开收到的邮件并进入任意 Case。
- [ ] 点击“模拟创建工单”后生成测试工单号。
- [ ] 结果区域显示“邮件附件：已上传”。
- [ ] 结果区域显示“业务执行：执行成功”。
- [ ] 临时目录中生成可正常打开的 `.msg` 文件。
- [ ] MSG 中保留原邮件格式和原始附件。
- [ ] 将 `attachCurrentMail` 改为 `false` 并重载后，结果显示附件未启用。
- [ ] 将 Helix 模式改为 `http` 并重载后，插件明确提示真实映射尚未配置，不发送请求。
- [ ] 将 Case API 模式改为 `http` 并重载后，工单号保留，同时显示业务执行失败。

## 后续待完成

- 根据用户提供的 Helix 固定字段映射实现真实开单请求。
- 根据 Helix 附件接口协议实现真实 MSG 上传。
- 根据各 Case API 的认证、请求和回参结构实现通用 HTTP 调用与参数映射。
- 根据 Dify/RAGFlow 工作流协议实现分类、参数提取和回复生成 Agent。
- 增加回复/全部回复草稿生成，并由人工确认后发送。
