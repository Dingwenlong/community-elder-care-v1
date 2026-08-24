[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^(?:\d{1,3}\.){3}\d{1,3}$')]
    [string]$LanIPv4,

    [int]$ApiPort = 5180
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$taskRepositoryRoot = Split-Path -Parent $PSScriptRoot
. (Join-Path $PSScriptRoot 'dev-env.ps1')

function Invoke-Adb {
    param([string[]]$Arguments)
    $taskOutput = & adb @Arguments 2>&1
    $taskExitCode = $LASTEXITCODE
    if ($taskExitCode -ne 0) {
        throw "ADB command failed with exit code $taskExitCode."
    }
    return @($taskOutput | ForEach-Object { $_.ToString() })
}

$taskAddress = [Net.IPAddress]::Parse($LanIPv4)
$taskOctets = $taskAddress.GetAddressBytes()
if ($taskAddress.AddressFamily -ne [Net.Sockets.AddressFamily]::InterNetwork -or
    $taskOctets[0] -eq 127 -or
    ($taskOctets[0] -eq 169 -and $taskOctets[1] -eq 254)) {
    throw 'LanIPv4 must be a non-loopback, non-APIPA IPv4 address.'
}
$taskHostAddress = Get-NetIPAddress -AddressFamily IPv4 -ErrorAction Stop |
    Where-Object { $_.IPAddress -eq $LanIPv4 -and $_.AddressState -eq 'Preferred' }
if ($null -eq $taskHostAddress) {
    throw "LanIPv4 $LanIPv4 is not an active preferred address on this laptop."
}

$taskDeviceLines = @(Invoke-Adb @('devices', '-l') | Where-Object {
    $_ -match '^\S+\s+device(?:\s|$)'
})
$taskPhysical = @()
foreach ($taskLine in $taskDeviceLines) {
    $taskSerial = ($taskLine -split '\s+')[0]
    $taskIsEmulator = (Invoke-Adb @('-s', $taskSerial, 'shell', 'getprop', 'ro.kernel.qemu') |
        Select-Object -First 1).Trim() -eq '1'
    if (-not $taskIsEmulator) {
        $taskPhysical += [pscustomobject]@{ Serial = $taskSerial; Line = $taskLine }
    }
}
if ($taskPhysical.Count -ne 1) {
    throw "Expected exactly one connected non-emulator Android device, found $($taskPhysical.Count)."
}
$taskDevice = $taskPhysical[0]

$taskHealthUrl = "http://${LanIPv4}:$ApiPort/health/ready"
$taskHostHealth = Invoke-RestMethod -Uri $taskHealthUrl -TimeoutSec 5
if ($taskHostHealth.status -ne 'ready') {
    throw "API readiness is not ready at $taskHealthUrl."
}

Push-Location (Join-Path $taskRepositoryRoot 'apps\mobile')
try {
    flutter build apk --debug --dart-define="API_BASE_URL=http://${LanIPv4}:$ApiPort"
    $taskBuildExitCode = $LASTEXITCODE
    if ($taskBuildExitCode -ne 0) {
        throw "Debug APK build failed with exit code $taskBuildExitCode."
    }
}
finally { Pop-Location }

$taskApk = Join-Path $taskRepositoryRoot 'apps\mobile\build\app\outputs\flutter-apk\app-debug.apk'
if (-not (Test-Path -LiteralPath $taskApk -PathType Leaf)) {
    throw "Built APK is missing: $taskApk"
}
[void](Invoke-Adb @('-s', $taskDevice.Serial, 'install', '-r', $taskApk))

$taskDeviceHealth = Invoke-Adb @(
    '-s', $taskDevice.Serial, 'shell', 'sh', '-c',
    "if command -v curl >/dev/null 2>&1; then curl -fsS '$taskHealthUrl'; elif toybox wget --help >/dev/null 2>&1; then toybox wget -qO- '$taskHealthUrl'; else exit 127; fi"
) | Out-String
if ($taskDeviceHealth -notmatch '"status"\s*:\s*"ready"') {
    throw 'The phone did not return ready JSON from /health/ready. No physical-phone receipt may be marked passed.'
}

[void](Invoke-Adb @(
    '-s', $taskDevice.Serial, 'shell', 'monkey', '-p',
    'com.dingwenlong.communitycare.mobile', '-c', 'android.intent.category.LAUNCHER', '1'
))
$taskModel = (Invoke-Adb @('-s', $taskDevice.Serial, 'shell', 'getprop', 'ro.product.model') |
    Select-Object -First 1).Trim()
$taskAndroid = (Invoke-Adb @('-s', $taskDevice.Serial, 'shell', 'getprop', 'ro.build.version.release') |
    Select-Object -First 1).Trim()
$taskHash = (Get-FileHash -LiteralPath $taskApk -Algorithm SHA256).Hash

Write-Output 'Physical phone automated gate passed.'
Write-Output "Device model: $taskModel"
Write-Output "Android version: $taskAndroid"
Write-Output "APK SHA-256: $taskHash"
Write-Output "API address class: private LAN IPv4 ($($taskOctets[0]).$($taskOctets[1]).x.x):$ApiPort"
Write-Output 'Manual receipt still requires elder login, check-in, offline/reconnect help, large-font layout and no-real-dial confirmation.'
