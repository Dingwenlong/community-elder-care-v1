param(
    [int]$ApiPort = 5180,
    [int]$WebPort = 5173
)

$ErrorActionPreference = 'Stop'
$taskRepositoryRoot = Split-Path -Parent $PSScriptRoot
$taskManifestPath = Join-Path $taskRepositoryRoot '.run\demo-processes.json'
$taskRunRoot = Split-Path -Parent $taskManifestPath

function Invoke-NativeStep([string]$Name, [scriptblock]$Command) {
    & $Command
    $taskNativeExitCode = $LASTEXITCODE
    if ($taskNativeExitCode -ne 0) {
        throw "$Name failed with exit code $taskNativeExitCode."
    }
}

function New-ProcessSecret([int]$ByteCount) {
    $taskBytes = [byte[]]::new($ByteCount)
    [Security.Cryptography.RandomNumberGenerator]::Fill($taskBytes)
    return [Convert]::ToBase64String($taskBytes)
}

if (Test-Path -LiteralPath $taskManifestPath) {
    throw "A demo process manifest already exists. Run scripts\stop-demo.ps1 before starting again."
}

. (Join-Path $PSScriptRoot 'dev-env.ps1')
Invoke-NativeStep 'Preflight' { & (Join-Path $PSScriptRoot 'preflight.ps1') }
Invoke-NativeStep 'API build' {
    dotnet build (Join-Path $taskRepositoryRoot 'src\CommunityElderCare.Api\CommunityElderCare.Api.csproj') --no-restore
}

$env:COMMUNITYCARE_DEMO_PASSWORD = New-ProcessSecret 24
$env:COMMUNITYCARE_JWT_SIGNING_KEY = New-ProcessSecret 48
if ([string]::IsNullOrWhiteSpace($env:COMMUNITYCARE_DEVICE_TOKEN)) {
    $env:COMMUNITYCARE_DEVICE_TOKEN = New-ProcessSecret 32
    $taskDeviceMode = 'simulator-only token generated'
}
else {
    $taskDeviceMode = 'operator-supplied physical token'
}
$env:VITE_API_PROXY_TARGET = "http://127.0.0.1:$ApiPort"
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS = "http://0.0.0.0:$ApiPort"

New-Item -ItemType Directory -Path $taskRunRoot -Force | Out-Null
$taskDotnet = (Get-Command dotnet -ErrorAction Stop).Source
$taskNode = (Get-Command node -ErrorAction Stop).Source
$taskApiDll = Join-Path $taskRepositoryRoot 'src\CommunityElderCare.Api\bin\Debug\net10.0\CommunityElderCare.Api.dll'
$taskViteEntry = Join-Path $taskRepositoryRoot 'apps\admin-web\node_modules\vite\bin\vite.js'
if (-not (Test-Path -LiteralPath $taskApiDll -PathType Leaf)) {
    throw "Built API entry point is missing: $taskApiDll"
}
if (-not (Test-Path -LiteralPath $taskViteEntry -PathType Leaf)) {
    throw "Web dependencies are missing: $taskViteEntry"
}

$taskApiProcess = $null
$taskWebProcess = $null
try {
    $taskApiProcess = Start-Process `
        -FilePath $taskDotnet `
        -ArgumentList @($taskApiDll) `
        -WorkingDirectory $taskRepositoryRoot `
        -WindowStyle Hidden `
        -RedirectStandardOutput (Join-Path $taskRunRoot 'api.stdout.log') `
        -RedirectStandardError (Join-Path $taskRunRoot 'api.stderr.log') `
        -PassThru
    $taskWebProcess = Start-Process `
        -FilePath $taskNode `
        -ArgumentList @($taskViteEntry, '--host', '0.0.0.0', '--port', $WebPort) `
        -WorkingDirectory (Join-Path $taskRepositoryRoot 'apps\admin-web') `
        -WindowStyle Hidden `
        -RedirectStandardOutput (Join-Path $taskRunRoot 'web.stdout.log') `
        -RedirectStandardError (Join-Path $taskRunRoot 'web.stderr.log') `
        -PassThru

    $taskManifest = [ordered]@{
        repositoryRoot = $taskRepositoryRoot
        createdAt = [DateTimeOffset]::Now.ToString('O')
        apiUrl = "http://127.0.0.1:$ApiPort"
        webUrl = "http://127.0.0.1:$WebPort"
        processes = @(
            [ordered]@{
                name = 'api'
                pid = $taskApiProcess.Id
                executablePath = $taskDotnet
                expectedCommandPath = $taskApiDll
            },
            [ordered]@{
                name = 'web'
                pid = $taskWebProcess.Id
                executablePath = $taskNode
                expectedCommandPath = $taskViteEntry
            }
        )
    }
    [IO.File]::WriteAllText(
        $taskManifestPath,
        ($taskManifest | ConvertTo-Json -Depth 5),
        [Text.UTF8Encoding]::new($false))

    $taskApiReady = $false
    $taskWebReady = $false
    for ($taskAttempt = 0; $taskAttempt -lt 120; $taskAttempt++) {
        if (-not $taskApiReady) {
            try {
                $taskReady = Invoke-RestMethod -Uri "http://127.0.0.1:$ApiPort/health/ready" -TimeoutSec 2
                $taskApiReady = $taskReady.status -eq 'ready'
            }
            catch { }
        }
        if (-not $taskWebReady) {
            try {
                $taskWebResponse = Invoke-WebRequest -Uri "http://127.0.0.1:$WebPort" -TimeoutSec 2
                $taskWebReady = $taskWebResponse.StatusCode -eq 200
            }
            catch { }
        }
        if ($taskApiReady -and $taskWebReady) { break }
        Start-Sleep -Milliseconds 500
    }
    if (-not $taskApiReady -or -not $taskWebReady) {
        throw "Demo readiness timed out. API ready=$taskApiReady; Web ready=$taskWebReady."
    }
}
catch {
    foreach ($taskProcess in @($taskApiProcess, $taskWebProcess)) {
        if ($null -ne $taskProcess -and -not $taskProcess.HasExited) {
            Stop-Process -Id $taskProcess.Id -Force -ErrorAction SilentlyContinue
        }
    }
    throw
}

Write-Output "Demo started: http://127.0.0.1:$WebPort"
Write-Output "API readiness: http://127.0.0.1:$ApiPort/health/ready"
Write-Output "Device mode: $taskDeviceMode"
Write-Output 'The generated demo password remains in COMMUNITYCARE_DEMO_PASSWORD for this PowerShell process.'
