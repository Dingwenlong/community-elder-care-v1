$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$htmlPath = Join-Path $projectRoot 'index.html'
$pdfPath = Join-Path $projectRoot '社区独居老人照料系统-项目开发全流程记录.pdf'
$chromePath = 'C:\Program Files\Google\Chrome\Application\chrome.exe'
$profilePath = Join-Path $env:TEMP 'community-care-pdf-chrome'

if (-not (Test-Path -LiteralPath $chromePath)) {
    throw "找不到 Chrome：$chromePath"
}

$url = [System.Uri]::new($htmlPath).AbsoluteUri
if (Test-Path -LiteralPath $profilePath) {
    Remove-Item -LiteralPath $profilePath -Recurse -Force
}

try {
    $arguments = @(
        '--headless=new'
        '--disable-gpu'
        '--no-pdf-header-footer'
        '--print-to-pdf-no-header'
        "--user-data-dir=$profilePath"
        "--print-to-pdf=$pdfPath"
        $url
    )
    $process = Start-Process -FilePath $chromePath -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
    if ($process.ExitCode -ne 0) {
        throw "Chrome 导出 PDF 失败，退出码：$($process.ExitCode)"
    }
}
finally {
    if (Test-Path -LiteralPath $profilePath) {
        Remove-Item -LiteralPath $profilePath -Recurse -Force
    }
}

if (-not (Test-Path -LiteralPath $pdfPath)) {
    throw 'Chrome 没有生成 PDF 文件。'
}

Get-Item -LiteralPath $pdfPath | Select-Object FullName, Length, LastWriteTime
