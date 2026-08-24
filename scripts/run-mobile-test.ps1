[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$TestPath
)

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
    $taskRoot = Split-Path -Parent $PSScriptRoot
    $taskMobileRoot = Join-Path $taskRoot 'apps\mobile'
    $taskRequestedTest = Join-Path $taskMobileRoot $TestPath
    $taskResolvedTest = (Resolve-Path -LiteralPath $taskRequestedTest -ErrorAction Stop).Path
    $taskResolvedMobileRoot = (Resolve-Path -LiteralPath $taskMobileRoot -ErrorAction Stop).Path
    if (-not $taskResolvedTest.StartsWith("$taskResolvedMobileRoot\", [StringComparison]::OrdinalIgnoreCase)) {
        throw 'TestPath must resolve inside apps/mobile.'
    }

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

    Push-Location $taskMobileRoot
    try {
        [void](Invoke-NativeCheck -Name "Flutter test '$TestPath'" -Command {
            flutter test $TestPath -d $taskAndroidEmulators[0].id
        })
    }
    finally {
        Pop-Location
    }

    Write-Host "Mobile test passed on $($taskAndroidEmulators[0].id): $TestPath"
}
catch {
    Write-Error "Mobile test failed: $($_.Exception.Message)"
    exit 1
}
