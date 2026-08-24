[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Invoke-NativeCheck {
    param(
        [Parameter(Mandatory)]
        [string]$Name,

        [Parameter(Mandatory)]
        [scriptblock]$Command
    )

    $taskOutput = & $Command 2>&1
    $taskExitCode = $LASTEXITCODE
    $taskText = ($taskOutput | ForEach-Object { $_.ToString() }) -join [Environment]::NewLine

    if ($taskExitCode -ne 0) {
        throw "$Name failed with exit code $taskExitCode.$([Environment]::NewLine)$taskText"
    }

    return $taskText.Trim()
}

try {
    $taskDotnetVersion = Invoke-NativeCheck -Name '.NET SDK' -Command { dotnet --version }
    if ($taskDotnetVersion -ne '10.0.302') {
        throw "Expected .NET SDK 10.0.302, found '$taskDotnetVersion'."
    }

    $taskNodeVersion = Invoke-NativeCheck -Name 'Node.js' -Command { node --version }
    if ($taskNodeVersion -ne 'v24.16.0') {
        throw "Expected Node.js v24.16.0, found '$taskNodeVersion'."
    }

    $taskFlutterVersion = Invoke-NativeCheck -Name 'Flutter' -Command { flutter --version }
    if ($taskFlutterVersion -notmatch '(?m)^Flutter 3\.47\.1 ') {
        throw 'Expected Flutter 3.47.1.'
    }

    $taskDartVersion = Invoke-NativeCheck -Name 'Dart' -Command { dart --version }
    if ($taskDartVersion -notmatch '^Dart SDK version: 3\.13\.1 ') {
        throw "Expected Dart 3.13.1, found '$taskDartVersion'."
    }

    [void](Invoke-NativeCheck -Name 'Java' -Command { java -version })
    [void](Invoke-NativeCheck -Name 'ADB' -Command { adb version })

    $taskAndroidSdk = $env:ANDROID_SDK_ROOT
    $taskAndroidJar = Join-Path $taskAndroidSdk 'platforms\android-36\android.jar'
    $taskBuildTools = Join-Path $taskAndroidSdk 'build-tools\36.0.0'
    if (-not (Test-Path -LiteralPath $taskAndroidJar -PathType Leaf)) {
        throw "Missing Android platform file: $taskAndroidJar"
    }
    if (-not (Test-Path -LiteralPath $taskBuildTools -PathType Container)) {
        throw "Missing Android build-tools directory: $taskBuildTools"
    }

    $taskRoot = Split-Path -Parent $PSScriptRoot
    $taskIntegrationProject = Join-Path $taskRoot 'tests\CommunityElderCare.IntegrationTests'
    [void](Invoke-NativeCheck -Name 'SQLite write canary' -Command {
        dotnet test $taskIntegrationProject --no-restore --filter FullyQualifiedName~Sqlite_can_write_temp_database --verbosity quiet
    })

    $taskDeviceJson = Invoke-NativeCheck -Name 'Flutter device discovery' -Command { flutter devices --machine }
    $taskDevices = @($taskDeviceJson | ConvertFrom-Json)
    $taskAndroidEmulators = @($taskDevices | Where-Object {
        $_.emulator -eq $true -and
        $_.isSupported -eq $true -and
        $_.targetPlatform -like 'android-*'
    })
    if ($taskAndroidEmulators.Count -ne 1) {
        throw "Expected exactly one supported online Android emulator, found $($taskAndroidEmulators.Count)."
    }

    Write-Host "Preflight passed: $($taskAndroidEmulators[0].id) is the sole supported Android emulator."
}
catch {
    Write-Error "Preflight failed: $($_.Exception.Message)"
    exit 1
}
