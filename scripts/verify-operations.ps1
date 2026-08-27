param(
    [switch]$Browser,
    [switch]$ConfirmDemoReset
)

$ErrorActionPreference = 'Stop'
$taskRepositoryRoot = Split-Path -Parent $PSScriptRoot

function Invoke-OperationsCheck([string]$Name, [scriptblock]$Command) {
    Write-Output "[operations] $Name"
    & $Command
    if ($LASTEXITCODE -ne 0) { throw "$Name failed with exit code $LASTEXITCODE." }
}

if ($Browser) {
    if (-not $ConfirmDemoReset) {
        throw 'Browser tests reset demo data. Use an isolated demo database and explicitly pass -ConfirmDemoReset.'
    }
    foreach ($taskVariable in @('COMMUNITYCARE_API_URL', 'COMMUNITYCARE_WEB_URL', 'COMMUNITYCARE_DEMO_PASSWORD')) {
        if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($taskVariable, 'Process'))) {
            throw "$taskVariable must be set in this process."
        }
    }
    foreach ($taskUrl in @($env:COMMUNITYCARE_API_URL, $env:COMMUNITYCARE_WEB_URL)) {
        $taskUri = [Uri]$taskUrl
        if (-not $taskUri.IsAbsoluteUri -or -not $taskUri.IsLoopback -or $taskUri.Scheme -notin @('http', 'https')) {
            throw 'Browser acceptance only targets an explicitly configured local demo environment.'
        }
    }
    $taskHealth = Invoke-RestMethod ($env:COMMUNITYCARE_API_URL.TrimEnd('/') + '/health/ready') -TimeoutSec 5
    if ($taskHealth.status -ne 'ready') { throw 'The isolated API is not ready.' }
    $null = Invoke-WebRequest $env:COMMUNITYCARE_WEB_URL -TimeoutSec 5
}

Push-Location $taskRepositoryRoot
try {
    Invoke-OperationsCheck '.NET tests' { dotnet test CommunityElderCare.sln --no-restore }
    Push-Location (Join-Path $taskRepositoryRoot 'apps/admin-web')
    try {
        Invoke-OperationsCheck 'Web tests' { npm test -- --run }
        Invoke-OperationsCheck 'Web oxlint' { npx --no-install oxlint . }
        Invoke-OperationsCheck 'Web ESLint' { npx --no-install eslint . }
        Invoke-OperationsCheck 'Web type check and build' { npm run build }
    }
    finally { Pop-Location }
    Push-Location (Join-Path $taskRepositoryRoot 'tests/e2e')
    try {
        Invoke-OperationsCheck 'Browser test type check' { npx --no-install tsc --noEmit }
        if ($Browser) {
            Invoke-OperationsCheck 'Main story, operations and authorization in Chromium' { npm test }
        }
        else {
            Invoke-OperationsCheck 'Browser test discovery only' { npx --no-install playwright test --list }
            Write-Output 'Browser execution was NOT performed. No services were started or stopped.'
        }
    }
    finally { Pop-Location }
}
finally { Pop-Location }

Write-Output 'Requested operations checks passed. Mobile and physical hardware acceptance are separate.'
