# 物理 Android 手机验收回执

状态：本轮跳过，未通过。

2026-08-25 本机没有连接非模拟器 Android 设备。用户确认本轮先跳过，因此没有执行安装、启动和手机侧 `/health/ready` 读取。下面这些字段目前没有可核验结果：

| 字段 | 记录 |
|---|---|
| 设备型号（脱敏） | 无记录 |
| Android 版本 | 无记录 |
| APK SHA-256 | 无实机安装回执 |
| API 地址类别 | 计划使用私有局域网 IPv4，不记录完整地址 |
| 老人登录与签到 | 未执行 |
| 离线求助与重连 | 未执行 |
| 大字体布局 | 未执行 |
| 无真实拨号 | 未执行 |

补测时运行：

```powershell
. .\scripts\dev-env.ps1
.\scripts\start-demo.ps1
.\scripts\verify-physical-phone.ps1 -LanIPv4 <笔记本局域网IPv4>
```

脚本只在恰好连接一台非模拟器 Android 设备、手机能读取 `/health/ready`、APK 安装成功并启动后通过。随后仍需人工完成表中的四项操作。回执不得写入完整设备序列号、Wi-Fi 密码、演示密码或设备令牌。
