$ErrorActionPreference = 'Stop'
$taskRepositoryRoot = Split-Path -Parent $PSScriptRoot
$taskManifestPath = Join-Path $taskRepositoryRoot '.run\demo-processes.json'
if (-not (Test-Path -LiteralPath $taskManifestPath -PathType Leaf)) {
    Write-Output 'No demo process manifest exists; nothing was stopped.'
    exit 0
}

$taskManifest = Get-Content -LiteralPath $taskManifestPath -Raw | ConvertFrom-Json
if (-not [string]::Equals(
    [IO.Path]::GetFullPath($taskManifest.repositoryRoot),
    [IO.Path]::GetFullPath($taskRepositoryRoot),
    [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Manifest repository root does not match this repository. No process was stopped.'
}

$taskUnsafe = @()
foreach ($taskEntry in $taskManifest.processes) {
    $taskProcess = Get-CimInstance Win32_Process -Filter "ProcessId = $($taskEntry.pid)" -ErrorAction SilentlyContinue
    if ($null -eq $taskProcess) {
        Write-Output "$($taskEntry.name) process is already stopped."
        continue
    }
    $taskExecutableMatches = [string]::Equals(
        [IO.Path]::GetFullPath($taskProcess.ExecutablePath),
        [IO.Path]::GetFullPath($taskEntry.executablePath),
        [StringComparison]::OrdinalIgnoreCase)
    $taskCommandMatches = $taskProcess.CommandLine -like "*$($taskEntry.expectedCommandPath)*" -and
        $taskProcess.CommandLine -like "*$taskRepositoryRoot*"
    if (-not $taskExecutableMatches -or -not $taskCommandMatches) {
        $taskUnsafe += $taskEntry.name
        Write-Warning "$($taskEntry.name) PID $($taskEntry.pid) no longer matches the recorded executable and repository command. It was not stopped."
        continue
    }
    Stop-Process -Id $taskEntry.pid -Force
    Write-Output "Stopped $($taskEntry.name) PID $($taskEntry.pid)."
}

if ($taskUnsafe.Count -gt 0) {
    throw "Refused to stop mismatched process entries: $($taskUnsafe -join ', '). Manifest retained."
}
Remove-Item -LiteralPath $taskManifestPath -Force
Write-Output 'Demo process manifest removed.'
