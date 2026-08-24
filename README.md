# 社区独居老人照料系统 v1

面向社区照护场景的参赛演示项目。v1 使用全量虚构档案和可重置演示数据，围绕独居老人的平安确认、求助上报、异常处置、家属知情和社区协同提供一条可现场演示的闭环。

## 项目组成

- `src/`：ASP.NET Core 10 API、领域模型与 SQLite 数据层。
- `apps/admin-web/`：Vue 3 社区工作台。
- `apps/mobile/`：Flutter Android 老人端与家属端演示应用。
- `firmware/esp32-sos/`：ESP32-S3 SOS 按钮固件（后续任务实现）。
- `docs/design/`：已确认的三端视觉概念与设计规则。

## 本机开发

在 PowerShell 中运行：

```powershell
. .\scripts\dev-env.ps1
.\scripts\preflight.ps1
dotnet test CommunityElderCare.sln
npm --prefix apps/admin-web test -- --run
npm --prefix apps/admin-web run build
.\scripts\run-mobile-test.ps1 -TestPath 'integration_test/app_shell_test.dart'
```

移动端脚本只接受恰好一台在线且受支持的 Android 模拟器，不会回退到 Windows 或浏览器目标。

## 数据边界

仓库和演示环境不得存放真实老人、家属、工作人员或设备资料。所有姓名、电话、健康风险、服务需求、位置和处置记录均由固定种子生成，并明确标记为“演示数据”。
