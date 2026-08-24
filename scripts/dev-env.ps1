$taskFlutterRoot = 'D:\Toolchains\flutter-3.47.1'
$taskAndroidSdk = Join-Path $env:LOCALAPPDATA 'Android\Sdk'
$taskJavaHome = 'D:\Toolchains\android-studio-2026.1.3.8\jbr'

$env:FLUTTER_ROOT = $taskFlutterRoot
$env:ANDROID_HOME = $taskAndroidSdk
$env:ANDROID_SDK_ROOT = $taskAndroidSdk
$env:JAVA_HOME = $taskJavaHome
$env:Path = "$taskFlutterRoot\bin;$taskJavaHome\bin;$taskAndroidSdk\platform-tools;$taskAndroidSdk\cmdline-tools\latest\bin;$env:Path"
