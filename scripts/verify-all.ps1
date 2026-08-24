param(
    [switch]$FailureCanary
)

$ErrorActionPreference = 'Stop'
$taskRepositoryRoot = Split-Path -Parent $PSScriptRoot

function Invoke-NativeStep([string]$Name, [scriptblock]$Command) {
    Write-Output "[verify] $Name"
    & $Command
    $taskNativeExitCode = $LASTEXITCODE
    if ($taskNativeExitCode -ne 0) {
        throw "$Name failed with exit code $taskNativeExitCode."
    }
}

try {
    . (Join-Path $PSScriptRoot 'dev-env.ps1')
    if ($FailureCanary) {
        Invoke-NativeStep 'failure canary' { & (Join-Path $PSHOME 'pwsh.exe') -NoProfile -Command 'exit 73' }
    }

    Invoke-NativeStep 'preflight' { & (Join-Path $PSScriptRoot 'preflight.ps1') }
    Invoke-NativeStep '.NET tests' { dotnet test (Join-Path $taskRepositoryRoot 'CommunityElderCare.sln') --no-restore }

    Push-Location (Join-Path $taskRepositoryRoot 'apps\admin-web')
    try {
        Invoke-NativeStep 'Web tests' { npm test -- --run }
        Invoke-NativeStep 'Web oxlint' { npx --no-install oxlint . }
        Invoke-NativeStep 'Web ESLint' { npx --no-install eslint . }
        Invoke-NativeStep 'Web build' { npm run build }
    }
    finally { Pop-Location }

    $taskDeviceJson = & flutter devices --machine
    $taskDeviceExitCode = $LASTEXITCODE
    if ($taskDeviceExitCode -ne 0) {
        throw "Flutter device discovery failed with exit code $taskDeviceExitCode."
    }
    $taskEmulators = @($taskDeviceJson | ConvertFrom-Json | Where-Object {
        $_.emulator -eq $true -and $_.isSupported -eq $true -and $_.targetPlatform -like 'android-*'
    })
    if ($taskEmulators.Count -ne 1) {
        throw "Expected exactly one supported Android emulator, found $($taskEmulators.Count)."
    }
    $taskEmulatorId = $taskEmulators[0].id

    Push-Location (Join-Path $taskRepositoryRoot 'apps\mobile')
    try {
        Invoke-NativeStep 'Android integration tests' { flutter test integration_test -d $taskEmulatorId }
        Invoke-NativeStep 'Flutter analyze' { flutter analyze }
        Invoke-NativeStep 'Debug APK build' {
            flutter build apk --debug --dart-define=API_BASE_URL=http://10.0.2.2:5180
        }
    }
    finally { Pop-Location }

    Invoke-NativeStep 'PlatformIO setup' { & (Join-Path $PSScriptRoot 'setup-platformio.ps1') }
    Invoke-NativeStep 'ESP32 compile' {
        & (Join-Path $taskRepositoryRoot '.tools\platformio\Scripts\platformio.exe') run `
            --project-dir (Join-Path $taskRepositoryRoot 'firmware\esp32-sos')
    }
    Invoke-NativeStep 'main-story API test' {
        dotnet test `
            (Join-Path $taskRepositoryRoot 'tests\CommunityElderCare.IntegrationTests') `
            --no-restore `
            --filter FullyQualifiedName~CareWorkEndpointTests.Visit_follow_up_and_event_form_a_guarded_closure_flow
    }

    Write-Output 'VERIFY ALL PASSED'
}
catch {
    Write-Error "VERIFY ALL FAILED: $($_.Exception.Message)"
    exit 1
}
