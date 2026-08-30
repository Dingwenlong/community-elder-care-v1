# 安邻照料——社区独居老人照料协同系统

“安邻照料”是一个参赛演示项目。它把独居老人的平安确认、求助、社区受理、上门探访、家属摘要和随访结案放进同一条事件记录。20 份老人档案全是固定种子生成的虚构数据，随时可以重置。

设计取舍和痛点来源见 [设计规格](docs/superpowers/specs/2026-08-24-community-elder-care-v1-design.md)。5 至 7 分钟现场流程见 [演示讲稿](docs/demo/demo-script.md)。

新增人员派单、设备台账、运营报表及 CSV / 打印功能，使用说明与复验方式见 [社区运营管理](docs/demo/community-operations.md)。

## 已实现的端

- `src/`：ASP.NET Core 10 API、领域规则、后台任务、审计和 SQLite 数据层。
- `apps/admin-web/`：Vue 3 社区与服务人员工作台。
- `apps/mobile/`：Flutter Android 老人端和家属端。
- `firmware/esp32-sos/`：ESP32 SOS、无用水活动和离线信号演示固件。
- `tests/e2e/`：Playwright 主故事和授权边界验收。

## 本机要求

固定版本为 .NET SDK 10.0.302、Node.js 24.16.0、Flutter 3.47.1、Dart 3.13.1、Android SDK 36 和 PlatformIO 6.1.19。准备一台在线 Android 模拟器。

复制 `scripts/dev-env.example.ps1` 中的三项设置到忽略目录 `.run/dev-env.local.ps1`，填入本机 Flutter、Android SDK 和 Java 路径。脚本只改当前 PowerShell 进程，不写系统环境变量。

## 启动演示

```powershell
. .\scripts\dev-env.ps1
.\scripts\start-demo.ps1
.\scripts\reset-demo.ps1
```

`start-demo.ps1` 会在当前进程生成演示密码、JWT 签名键和模拟设备令牌，不把它们写入进程清单或日志。后台账号为 `community.demo`、`service.demo` 和 `admin.demo`；老人端与家属端账号为 `elder.demo` 和 `family.demo`。密码只从当前运行环境取得。

结束后运行：

```powershell
.\scripts\stop-demo.ps1
```

## 验证

```powershell
.\scripts\verify-all.ps1
try {
  .\scripts\start-demo.ps1
  npm --prefix tests/e2e test
}
finally {
  .\scripts\stop-demo.ps1
}
```

`verify-all.ps1` 会检查 .NET、Web、Android 模拟器、Flutter APK 和 ESP32 编译。它要求恰好一台受支持的 Android 模拟器，不会回退到 Windows 或浏览器。物理手机必须另跑 `verify-physical-phone.ps1`；当前回执在 [物理手机验收](docs/demo/physical-phone-receipt.md)。

## ESP32 实物演示

在同一个 PowerShell 进程中设置 Wi-Fi、API 地址和设备令牌，再编译烧录并启动 API。不要把这些值写进仓库。

```powershell
$env:COMMUNITYCARE_WIFI_SSID = '<现场 Wi-Fi 名称>'
$env:COMMUNITYCARE_WIFI_PASSWORD = '<现场 Wi-Fi 密码>'
$env:COMMUNITYCARE_API_BASE_URL = 'http://<笔记本局域网IPv4>:5180'
$env:COMMUNITYCARE_DEVICE_TOKEN = '<随机设备令牌>'
.\scripts\setup-platformio.ps1 -Physical
.\.tools\platformio\Scripts\platformio.exe run --project-dir .\firmware\esp32-sos --target upload
.\scripts\start-demo.ps1
```

## 打包

```powershell
.\scripts\package-demo.ps1
```

脚本先执行全量门禁，再把 API 发布目录、Web 构建、Debug APK、固件、公开文档和安全脚本写入忽略目录 `artifacts/demo-v1/`，最后生成 `SHA256SUMS.txt`。日志、数据库、令牌、`.env`、`.run`、工具缓存和本机路径会被拒绝。

## 安全边界

仓库和演示环境不得存放真实老人、家属、工作人员或设备资料。系统不做医疗诊断，不替代社区人员，也不连接真实短信、电话或急救服务。AI 原始对话和社区内部探访笔记不进入家属响应或审计页面。
