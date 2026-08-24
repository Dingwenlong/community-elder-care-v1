param(
    [switch]$Physical
)

$ErrorActionPreference = 'Stop'
$taskRepositoryRoot = Split-Path -Parent $PSScriptRoot
$taskToolRoot = Join-Path $taskRepositoryRoot '.tools\platformio'
$taskPython = Join-Path $taskToolRoot 'Scripts\python.exe'
$taskPlatformIo = Join-Path $taskToolRoot 'Scripts\platformio.exe'
$taskFirmwareRoot = Join-Path $taskRepositoryRoot 'firmware\esp32-sos'
$taskExampleConfig = Join-Path $taskFirmwareRoot 'include\demo_config.example.h'
$taskActiveConfig = Join-Path $taskFirmwareRoot 'include\demo_config.h'

if (-not (Test-Path -LiteralPath $taskPython)) {
    $taskSystemPython = Get-Command python -ErrorAction Stop
    & $taskSystemPython.Source -m venv $taskToolRoot
    if ($LASTEXITCODE -ne 0) {
        throw 'Failed to create the repository-local PlatformIO virtual environment.'
    }
}

$taskInstalledVersion = $null
if (Test-Path -LiteralPath $taskPlatformIo) {
    $taskVersionOutput = & $taskPlatformIo --version 2>$null
    if ($LASTEXITCODE -eq 0 -and $taskVersionOutput -match '6\.1\.19') {
        $taskInstalledVersion = '6.1.19'
    }
}
if ($taskInstalledVersion -ne '6.1.19') {
    & $taskPython -m pip install --disable-pip-version-check --no-input 'platformio==6.1.19'
    if ($LASTEXITCODE -ne 0) {
        throw 'Failed to install PlatformIO 6.1.19 in the repository-local environment.'
    }
}

if ($Physical) {
    $taskRequiredValues = [ordered]@{
        COMMUNITYCARE_WIFI_SSID = $env:COMMUNITYCARE_WIFI_SSID
        COMMUNITYCARE_WIFI_PASSWORD = $env:COMMUNITYCARE_WIFI_PASSWORD
        COMMUNITYCARE_API_BASE_URL = $env:COMMUNITYCARE_API_BASE_URL
        COMMUNITYCARE_DEVICE_TOKEN = $env:COMMUNITYCARE_DEVICE_TOKEN
    }
    $taskMissingNames = @($taskRequiredValues.Keys | Where-Object {
        [string]::IsNullOrWhiteSpace($taskRequiredValues[$_])
    })
    if ($taskMissingNames.Count -gt 0) {
        throw "Physical mode requires process-local values for: $($taskMissingNames -join ', ')."
    }
    if ($taskRequiredValues.Values | Where-Object { $_ -match '[\r\n]' }) {
        throw 'Physical-mode values cannot contain line breaks.'
    }
    if ($taskRequiredValues.Values | Where-Object { $_ -match 'COMPILE_ONLY_' }) {
        throw 'Physical mode rejects public compile-only placeholder values.'
    }
    $taskParsedUrl = $null
    if (-not [Uri]::TryCreate(
        $taskRequiredValues.COMMUNITYCARE_API_BASE_URL,
        [UriKind]::Absolute,
        [ref]$taskParsedUrl) -or
        $taskParsedUrl.Scheme -notin @('http', 'https')) {
        throw 'COMMUNITYCARE_API_BASE_URL must be an absolute HTTP or HTTPS URL.'
    }
    if ($taskParsedUrl.Host -match '^192\.0\.2\.') {
        throw 'Physical mode rejects the documentation-only 192.0.2.0/24 address range.'
    }

    function ConvertTo-CppLiteral([string]$Value) {
        return $Value.Replace('\', '\\').Replace('"', '\"')
    }

    $taskCandidate = @(
        '#pragma once',
        '',
        "#define COMMUNITYCARE_WIFI_SSID `"$(ConvertTo-CppLiteral $taskRequiredValues.COMMUNITYCARE_WIFI_SSID)`"",
        "#define COMMUNITYCARE_WIFI_PASSWORD `"$(ConvertTo-CppLiteral $taskRequiredValues.COMMUNITYCARE_WIFI_PASSWORD)`"",
        "#define COMMUNITYCARE_API_BASE_URL `"$(ConvertTo-CppLiteral $taskRequiredValues.COMMUNITYCARE_API_BASE_URL.TrimEnd('/'))`"",
        "#define COMMUNITYCARE_DEVICE_TOKEN `"$(ConvertTo-CppLiteral $taskRequiredValues.COMMUNITYCARE_DEVICE_TOKEN)`"",
        '#define COMMUNITYCARE_DEVICE_ID "77777777-7777-7777-7777-777777777701"'
    )
    $taskTemporaryConfig = [IO.Path]::GetTempFileName()
    try {
        [IO.File]::WriteAllLines(
            $taskTemporaryConfig,
            $taskCandidate,
            [Text.UTF8Encoding]::new($false))
        $taskReadback = [IO.File]::ReadAllLines($taskTemporaryConfig)
        if ($taskReadback.Count -ne $taskCandidate.Count -or
            ($taskReadback -join "`n") -ne ($taskCandidate -join "`n")) {
            throw 'Generated physical device configuration failed readback verification.'
        }
        Move-Item -LiteralPath $taskTemporaryConfig -Destination $taskActiveConfig -Force
    }
    finally {
        if (Test-Path -LiteralPath $taskTemporaryConfig) {
            Remove-Item -LiteralPath $taskTemporaryConfig -Force
        }
    }
    Write-Output 'Prepared ignored physical-device configuration without displaying secret values.'
}
else {
    Copy-Item -LiteralPath $taskExampleConfig -Destination $taskActiveConfig -Force
    Write-Output 'Prepared compile-only configuration with non-working public values.'
}

& $taskPlatformIo --version
if ($LASTEXITCODE -ne 0) {
    throw 'Repository-local PlatformIO executable did not start successfully.'
}
Write-Output "PlatformIO is ready at $taskPlatformIo"
