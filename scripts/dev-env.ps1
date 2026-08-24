$ErrorActionPreference = 'Stop'
$taskRepositoryRoot = Split-Path -Parent $PSScriptRoot
$taskLocalConfig = Join-Path $taskRepositoryRoot '.run\dev-env.local.ps1'

if (Test-Path -LiteralPath $taskLocalConfig -PathType Leaf) {
    . $taskLocalConfig
}

function Resolve-ToolRoot {
    param(
        [string]$ConfiguredRoot,
        [string]$CommandName,
        [int]$ParentLevels = 1
    )

    if (-not [string]::IsNullOrWhiteSpace($ConfiguredRoot)) {
        return [IO.Path]::GetFullPath($ConfiguredRoot)
    }
    $taskCommand = Get-Command $CommandName -ErrorAction SilentlyContinue
    if ($null -eq $taskCommand) { return $null }
    $taskRoot = Split-Path -Parent $taskCommand.Source
    for ($taskLevel = 1; $taskLevel -lt $ParentLevels; $taskLevel++) {
        $taskRoot = Split-Path -Parent $taskRoot
    }
    return $taskRoot
}

$taskFlutterRoot = Resolve-ToolRoot `
    -ConfiguredRoot ($env:COMMUNITYCARE_FLUTTER_ROOT ?? $env:FLUTTER_ROOT) `
    -CommandName 'flutter' `
    -ParentLevels 2
$taskAndroidSdk = $env:COMMUNITYCARE_ANDROID_SDK_ROOT ?? `
    $env:ANDROID_SDK_ROOT ?? `
    $env:ANDROID_HOME
if ([string]::IsNullOrWhiteSpace($taskAndroidSdk)) {
    $taskAndroidSdk = Join-Path $env:LOCALAPPDATA 'Android\Sdk'
}
$taskJavaHome = Resolve-ToolRoot `
    -ConfiguredRoot ($env:COMMUNITYCARE_JAVA_HOME ?? $env:JAVA_HOME) `
    -CommandName 'java' `
    -ParentLevels 2

$taskMissing = @()
if ([string]::IsNullOrWhiteSpace($taskFlutterRoot) -or
    -not (Test-Path -LiteralPath (Join-Path $taskFlutterRoot 'bin\flutter.bat') -PathType Leaf)) {
    $taskMissing += 'COMMUNITYCARE_FLUTTER_ROOT'
}
if ([string]::IsNullOrWhiteSpace($taskAndroidSdk) -or
    -not (Test-Path -LiteralPath $taskAndroidSdk -PathType Container)) {
    $taskMissing += 'COMMUNITYCARE_ANDROID_SDK_ROOT'
}
if ([string]::IsNullOrWhiteSpace($taskJavaHome) -or
    -not (Test-Path -LiteralPath (Join-Path $taskJavaHome 'bin\java.exe') -PathType Leaf)) {
    $taskMissing += 'COMMUNITYCARE_JAVA_HOME'
}
if ($taskMissing.Count -gt 0) {
    throw "Missing local tool configuration: $($taskMissing -join ', '). Copy scripts\dev-env.example.ps1 values into ignored .run\dev-env.local.ps1 or set process environment variables."
}

$env:FLUTTER_ROOT = $taskFlutterRoot
$env:ANDROID_HOME = [IO.Path]::GetFullPath($taskAndroidSdk)
$env:ANDROID_SDK_ROOT = $env:ANDROID_HOME
$env:JAVA_HOME = $taskJavaHome
$taskToolPaths = @(
    (Join-Path $taskFlutterRoot 'bin'),
    (Join-Path $taskJavaHome 'bin'),
    (Join-Path $env:ANDROID_HOME 'platform-tools'),
    (Join-Path $env:ANDROID_HOME 'cmdline-tools\latest\bin')
)
$taskCurrentPaths = $env:Path -split [IO.Path]::PathSeparator
$env:Path = (@($taskToolPaths + $taskCurrentPaths) |
    Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
    Select-Object -Unique) -join [IO.Path]::PathSeparator
