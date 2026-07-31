# OutlookOpsAssistant 外部配置

## 目的

测试环境、生产环境的 NAS、Agent、Helix 和 Case API 条件不同。插件不把这些环境参数写死在 DLL 中，切换环境时只修改外部 JSON，不需要重新编译。

## Bootstrap 查找顺序

插件启动或点击“重新加载配置”时，按以下顺序寻找 `bootstrap.json`，找到第一个可正常读取的文件后停止：

1. 环境变量 `OUTLOOK_OPS_ASSISTANT_BOOTSTRAP` 指定的文件。
2. `%ProgramData%\OutlookOpsAssistant\bootstrap.json`。
3. `%LocalAppData%\OutlookOpsAssistant\bootstrap.json`。
4. 插件目录下的 `config\bootstrap.json`。

开发阶段默认使用项目中的 `OutlookOpsAssistant\config\bootstrap.json`。

生产切换时，推荐在 `%ProgramData%\OutlookOpsAssistant\bootstrap.json` 放一份机器级配置；没有管理员写权限时，可使用 `%LocalAppData%` 或环境变量。

## bootstrap.json

当前测试配置示例：

```json
{
  "environment": "test",
  "caseConfigPath": "cases.json",
  "agent": {
    "mode": "mock",
    "classificationEndpoint": "",
    "extractionEndpoint": "",
    "replyEndpoint": "",
    "timeoutSeconds": 30
  },
  "api": {
    "enabled": true,
    "mode": "mock",
    "baseUrl": "",
    "timeoutSeconds": 30,
    "helix": {
      "mode": "mock",
      "baseUrl": "",
      "createTicketEndpoint": "",
      "attachmentEndpoint": "",
      "timeoutSeconds": 30,
      "attachCurrentMail": true,
      "mailExportDirectory": "%LOCALAPPDATA%\\OutlookOpsAssistant\\temp",
      "deleteExportedMailAfterUpload": false
    }
  },
  "paths": {
    "cacheDirectory": "%LOCALAPPDATA%\\OutlookOpsAssistant\\cache",
    "logDirectory": "%LOCALAPPDATA%\\OutlookOpsAssistant\\logs"
  }
}
```

`caseConfigPath` 支持绝对路径、UNC 路径、环境变量和相对于 `bootstrap.json` 的相对路径。

## Agent 配置

Agent 已按能力拆分地址：

- `classificationEndpoint`：Case 分类。
- `extractionEndpoint`：选定 Case 后的参数提取。
- `replyEndpoint`：根据邮件、工单和执行结果生成回复草稿。

当前只有 `mock` 模式可运行。真实 Agent 请求和回参协议需在生产环境确认后接入；改为其他模式时不会静默退回 Mock。

## Case API 配置

`api.enabled` 控制是否执行各 Case 的业务 API。

- `mode`：当前测试使用 `mock`，生产预留 `http`。
- `baseUrl`：Case API 公共基础地址。
- `timeoutSeconds`：公共超时时间。

同时还要求对应 Case 的 `api.enabled=true`，才会执行该 Case 的业务 API。

当前已经完成服务接口和 Mock 编排。真实 HTTP 请求映射需要根据每个 Case 的接口协议继续配置和实现。

## Helix 配置

Helix 与 Case API 是两套独立服务：

- Helix：创建工单、上传工单附件。
- Case API：执行具体业务动作。

Helix 设置：

- `mode`：当前为 `mock`，生产预留 `http`。
- `baseUrl`：Helix 基础地址。
- `createTicketEndpoint`：开单接口地址。
- `attachmentEndpoint`：附件上传接口地址。
- `attachCurrentMail`：是否把当前邮件导出为 `.msg` 并上传。
- `mailExportDirectory`：MSG 临时文件目录。
- `deleteExportedMailAfterUpload`：上传结束后是否删除临时文件。

测试配置暂时将 `deleteExportedMailAfterUpload` 设置为 `false`，方便检查实际生成的 MSG 文件。验证完成后应改为 `true`，避免长期积累临时文件。

附件失败不会把已经创建成功的工单改成失败。结果会明确显示“工单已创建，但邮件附件上传失败”。

## 生产配置示例

```json
{
  "environment": "prod",
  "caseConfigPath": "\\\\NAS01\\OpsAssistant\\config\\cases.json",
  "agent": {
    "mode": "http",
    "classificationEndpoint": "http://internal-agent/case-classify",
    "extractionEndpoint": "http://internal-agent/case-extract",
    "replyEndpoint": "http://internal-agent/reply-draft",
    "timeoutSeconds": 60
  },
  "api": {
    "enabled": true,
    "mode": "http",
    "baseUrl": "http://internal-case-api",
    "timeoutSeconds": 60,
    "helix": {
      "mode": "http",
      "baseUrl": "http://internal-helix-api",
      "createTicketEndpoint": "/tickets",
      "attachmentEndpoint": "/tickets/{ticketId}/attachments",
      "timeoutSeconds": 60,
      "attachCurrentMail": true,
      "mailExportDirectory": "%LOCALAPPDATA%\\OutlookOpsAssistant\\temp",
      "deleteExportedMailAfterUpload": true
    }
  },
  "paths": {
    "cacheDirectory": "%LOCALAPPDATA%\\OutlookOpsAssistant\\cache",
    "logDirectory": "%LOCALAPPDATA%\\OutlookOpsAssistant\\logs"
  }
}
```

该生产示例只说明配置结构。真实 Helix、Agent 和 Case API 协议未确认前，不应直接启用生产 HTTP 调用。

## Case 配置

`cases.json` 中每个 Case 可以配置：

- 是否启用、Code、名称和说明；
- 匹配关键词；
- 动态表单字段；
- 结果展示字段；
- Case API 的相对或绝对地址、方法和超时。

字段类型支持：`text`、`multiline`、`select`。

结果类型支持：`text`、`status`、`multiline`、`list`、`table`。

当前测试 Case 已增加以下结果字段：

- 邮件附件状态：`Data.mailAttachmentStatus`。
- Case API 状态：`Data.caseApiStatus`。

## 当前执行顺序

```text
Helix 创建工单
→ 导出当前邮件为 MSG
→ 上传 MSG 到该工单
→ 执行 Case API
→ 合并并展示处理结果
```

如果 Case API 失败，插件保留已经创建的工单号，并显示“工单已创建，但 Case API 执行失败”。

## 回退策略

1. 优先读取 `caseConfigPath`。
2. 读取成功后保存到 `%LocalAppData%\OutlookOpsAssistant\cache\cases.cache.json`。
3. 主配置不可用时读取本地缓存。
4. 主配置和缓存都不可用时，使用插件内置的最小保底 Case。

## 运行时操作

- **重新加载配置**：修改 JSON 后，无需重新编译插件；打开任务窗格并点击该按钮即可重新读取，并重建 Agent、Helix 和 Case API 服务选择。
- **配置诊断**：查看实际使用的 Bootstrap、Case 文件、Case 数量、Agent、Helix、Case API、邮件导出目录、缓存目录和读取警告。

## 安全约束

API Key、密码和生产凭据不要写入普通 JSON。后续应使用 Windows 凭据管理器或由内部网关统一持有密钥。
