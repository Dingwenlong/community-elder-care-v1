# ESP32 SOS 演示固件

该固件用于参赛演示：GPIO 0 按钮消抖后必须持续按住 2 秒，才会生成 UUID 格式的事件编号并上报 `SosButton` 信号。GPIO 2 LED 常亮表示发送中；短闪两次表示服务端已接收；慢闪三次表示本轮有限重试失败。失败不会改变事件编号，因而服务端能够幂等处理重试。

## 仅编译验证

在仓库根目录运行：

```powershell
.\scripts\setup-platformio.ps1
.\.tools\platformio\Scripts\platformio.exe run --project-dir firmware\esp32-sos
```

默认会把 `include/demo_config.example.h` 复制为被 Git 忽略的 `demo_config.h`。其中的 Wi-Fi、密码和令牌均为明显不可用的编译占位值，`192.0.2.1` 属于文档示例地址；这一步只证明固件可以编译，不会连接演示后端。

## 连接物理演示板

先在当前 PowerShell 进程设置以下四个环境变量，再显式启用物理模式：

```powershell
$env:COMMUNITYCARE_WIFI_SSID = '<演示 Wi-Fi>'
$env:COMMUNITYCARE_WIFI_PASSWORD = '<演示 Wi-Fi 密码>'
$env:COMMUNITYCARE_API_BASE_URL = 'http://<演示电脑局域网地址>:5180'
$env:COMMUNITYCARE_DEVICE_TOKEN = '<与服务端当前进程一致的设备令牌>'
.\scripts\setup-platformio.ps1 -Physical
```

脚本不会打印这些值，只写入被忽略的本地头文件。后端必须在同一进程环境下使用相同的 `COMMUNITYCARE_DEVICE_TOKEN` 启动。手机或硬件访问电脑时应使用局域网地址，并在防火墙中仅按演示需要开放端口。

公开的编译占位值绝不能用于保护真实设备或真实服务。不要提交 `demo_config.h`，不要复用比赛现场令牌，也不要在串口日志中输出 Wi-Fi 密码或设备令牌。GPIO 0 同时是常见启动模式引脚：烧录或复位期间不要持续按住，设备启动后再进行 2 秒长按演示。
