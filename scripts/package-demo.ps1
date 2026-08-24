[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$taskRepositoryRoot = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$taskArtifactRoot = [IO.Path]::GetFullPath((Join-Path $taskRepositoryRoot 'artifacts\demo-v1'))
$taskArtifactParent = [IO.Path]::GetFullPath((Join-Path $taskRepositoryRoot 'artifacts'))
if (-not $taskArtifactRoot.StartsWith($taskArtifactParent + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Artifact path escaped the repository artifact directory.'
}

function Invoke-NativeStep {
    param([string]$Name, [scriptblock]$Command)
    & $Command
    $taskExitCode = $LASTEXITCODE
    if ($taskExitCode -ne 0) { throw "$Name failed with exit code $taskExitCode." }
}

function Assert-RepositorySource {
    param([string]$Path)
    $taskResolved = [IO.Path]::GetFullPath($Path)
    if (-not $taskResolved.StartsWith($taskRepositoryRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Package source escaped the repository: $Path"
    }
    if (-not (Test-Path -LiteralPath $taskResolved)) {
        throw "Package source is missing: $Path"
    }
    return $taskResolved
}

function Copy-SafeItem {
    param([string]$Source, [string]$Destination)
    $taskSource = Assert-RepositorySource $Source
    $taskDestination = Join-Path $taskArtifactRoot $Destination
    $taskDestinationParent = Split-Path -Parent $taskDestination
    New-Item -ItemType Directory -Path $taskDestinationParent -Force | Out-Null
    Copy-Item -LiteralPath $taskSource -Destination $taskDestination -Recurse -Force
}

. (Join-Path $PSScriptRoot 'dev-env.ps1')
Invoke-NativeStep 'Full verification' { & (Join-Path $PSScriptRoot 'verify-all.ps1') }

if (Test-Path -LiteralPath $taskArtifactRoot) {
    $taskResolvedExisting = [IO.Path]::GetFullPath((Resolve-Path -LiteralPath $taskArtifactRoot))
    if ($taskResolvedExisting -ne $taskArtifactRoot) {
        throw 'Resolved artifact target does not match the expected exact directory.'
    }
    Remove-Item -LiteralPath $taskArtifactRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $taskArtifactRoot -Force | Out-Null

$taskApiOutput = Join-Path $taskArtifactRoot 'api'
Invoke-NativeStep 'API publish' {
    dotnet publish `
        (Join-Path $taskRepositoryRoot 'src\CommunityElderCare.Api\CommunityElderCare.Api.csproj') `
        --configuration Release `
        --no-restore `
        --output $taskApiOutput
}
Get-ChildItem -LiteralPath $taskApiOutput -Filter '*.pdb' -File -Recurse |
    Remove-Item -Force
Copy-SafeItem (Join-Path $taskRepositoryRoot 'apps\admin-web\dist') 'web'
Copy-SafeItem (Join-Path $taskRepositoryRoot 'apps\mobile\build\app\outputs\flutter-apk\app-debug.apk') 'mobile\community-care-demo-v1.apk'
Copy-SafeItem (Join-Path $taskRepositoryRoot 'firmware\esp32-sos\.pio\build\esp32dev\firmware.bin') 'firmware\esp32-sos-demo-v1.bin'
foreach ($taskScript in @(
    'dev-env.ps1',
    'dev-env.example.ps1',
    'preflight.ps1',
    'reset-demo.ps1',
    'setup-platformio.ps1',
    'start-demo.ps1',
    'stop-demo.ps1',
    'verify-all.ps1',
    'verify-physical-phone.ps1'
)) {
    Copy-SafeItem (Join-Path $PSScriptRoot $taskScript) (Join-Path 'scripts' $taskScript)
}
Copy-SafeItem (Join-Path $taskRepositoryRoot 'docs\demo') 'docs\demo'
Copy-SafeItem (Join-Path $taskRepositoryRoot 'docs\progress\release-checklist.md') 'docs\release-checklist.md'
Copy-SafeItem (Join-Path $taskRepositoryRoot 'README.md') 'README.md'

$taskForbiddenPathPattern = '(?i)(^|[\\/])(\.run|\.tools|node_modules|\.pio)([\\/]|$)|\.env|\.db(?:-shm|-wal)?$|\.log$|\.pdb$'
$taskBadPaths = @(Get-ChildItem -LiteralPath $taskArtifactRoot -File -Recurse | Where-Object {
    $_.FullName.Substring($taskArtifactRoot.Length + 1) -match $taskForbiddenPathPattern
})
if ($taskBadPaths.Count -gt 0) {
    throw "Forbidden artifact path detected: $($taskBadPaths[0].FullName)"
}

$taskTextExtensions = @('.json', '.md', '.ps1', '.html', '.js', '.css', '.txt', '.config')
$taskSecretPattern = 'C:\\Users\\|D:\\Workspace\\|gh[pousr]_[A-Za-z0-9]{20,}|sk-[A-Za-z0-9_-]{16,}|BEGIN (RSA|OPENSSH|EC) PRIVATE KEY|COMMUNITYCARE_(DEMO_PASSWORD|JWT_SIGNING_KEY|DEVICE_TOKEN)\s*=\s*["''][^<$]'
foreach ($taskFile in Get-ChildItem -LiteralPath $taskArtifactRoot -File -Recurse) {
    if ($taskTextExtensions -contains $taskFile.Extension.ToLowerInvariant()) {
        if ((Get-Content -LiteralPath $taskFile.FullName -Raw) -match $taskSecretPattern) {
            throw "Sensitive value or internal path detected in artifact: $($taskFile.FullName)"
        }
    }
}

$taskManifestPath = Join-Path $taskArtifactRoot 'SHA256SUMS.txt'
$taskManifestLines = Get-ChildItem -LiteralPath $taskArtifactRoot -File -Recurse |
    Where-Object { $_.FullName -ne $taskManifestPath } |
    ForEach-Object {
        $taskRelative = $_.FullName.Substring($taskArtifactRoot.Length + 1).Replace('\', '/')
        $taskHash = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        "$taskHash  $taskRelative"
    } |
    Sort-Object
[IO.File]::WriteAllLines($taskManifestPath, $taskManifestLines, [Text.UTF8Encoding]::new($false))
if ($taskManifestLines.Count -eq 0) { throw 'Artifact manifest is empty.' }

Write-Output "Demo package ready: $taskArtifactRoot"
Write-Output "Manifest entries: $($taskManifestLines.Count)"
