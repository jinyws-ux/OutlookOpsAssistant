# OutlookOpsAssistant 外部配置

## 目的

测试环境、生产环境的 NAS、Agent 和 API 条件不同。插件不把这些环境参数写死在 DLL 中，切换环境时只修改外部 JSON，不需要重新编译。

## Bootstrap 查找顺序

插件启动或点击“重新加载配置”时，按以下顺序寻找 `bootstrap.json`，找到第一个可正常读取的文件后停止：

1. 环境变量 `OUTLOOK_OPS_ASSISTANT_BOOTSTRAP` 指定的文件。
2. `%ProgramData%\OutlookOpsAssistant\bootstrap.json`。
3. `%LocalAppData%\OutlookOpsAssistant\bootstrap.json`。
4. 插件目录下的 `config\bootstrap.json`。

开发阶段默认使用项目中的 `OutlookOpsAssistant\config\bootstrap.json`。

生产切换时，推荐在 `%ProgramData%\OutlookOpsAssistant\bootstrap.json` 放一份机器级配置；没有管理员写权限时，可使用 `%LocalAppData%` 或环境变量。

## bootstrap.json

```json
{
  "environment": "test",
  "caseConfigPath": "cases.json",
  "agent": {
    "mode": "mock",
    "classificationEndpoint": "",
    "extractionEndpoint": "",
    "timeoutSeconds": 30
  },
  "api": {
    "enabled": false,
    "baseUrl": "",
    "timeoutSeconds": 30
  },
  "paths": {
    "cacheDirectory": "%LOCALAPPDATA%\\OutlookOpsAssistant\\cache",
    "logDirectory": "%LOCALAPPDATA%\\OutlookOpsAssistant\\logs"
  }
}
```

`caseConfigPath` 支持绝对路径、UNC 路径、环境变量和相对于 `bootstrap.json` 的相对路径。

生产示例：

```json
{
  "environment": "prod",
  "caseConfigPath": "\\\\NAS01\\OpsAssistant\\config\\cases.json",
  "agent": {
    "mode": "http",
    "classificationEndpoint": "http://internal-agent/case-classify",
    "extractionEndpoint": "http://internal-agent/case-extract",
    "timeoutSeconds": 60
  },
  "api": {
    "enabled": true,
    "baseUrl": "http://internal-api",
    "timeoutSeconds": 60
  },
  "paths": {
    "cacheDirectory": "%LOCALAPPDATA%\\OutlookOpsAssistant\\cache",
    "logDirectory": "%LOCALAPPDATA%\\OutlookOpsAssistant\\logs"
  }
}
```

当前版本已经识别这些设置，但真实 HTTP Agent 和业务 API 尚未接入。将 Agent 模式改为非 `mock` 时，插件会明确提示该模式尚不可用，不会静默使用 Mock。

## Case 配置

`cases.json` 中每个 Case 可以配置：

- 是否启用、Code、名称和说明；
- 匹配关键词；
- 动态表单字段；
- 结果展示字段；
- Case API 的相对或绝对地址、方法和超时。

字段类型支持：`text`、`multiline`、`select`。

结果类型支持：`text`、`status`、`multiline`、`list`、`table`。

## 回退策略

1. 优先读取 `caseConfigPath`。
2. 读取成功后保存到 `%LocalAppData%\OutlookOpsAssistant\cache\cases.cache.json`。
3. 主配置不可用时读取本地缓存。
4. 主配置和缓存都不可用时，使用插件内置的最小保底 Case。

## 运行时操作

- **重新加载配置**：修改 JSON 后，无需重新编译插件；打开任务窗格并点击该按钮即可重新读取。
- **配置诊断**：查看实际使用的 Bootstrap、Case 文件、Case 数量、Agent/API 设置、缓存目录和读取警告。

## 安全约束

API Key、密码和生产凭据不要写入普通 JSON。后续应使用 Windows 凭据管理器或由内部网关统一持有密钥。
