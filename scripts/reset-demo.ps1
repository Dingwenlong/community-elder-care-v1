param(
    [string]$ApiBaseUrl = 'http://127.0.0.1:5180',
    [string]$AdminPassword = $env:COMMUNITYCARE_DEMO_PASSWORD
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($AdminPassword)) {
    throw 'Admin password is required through -AdminPassword or process-local COMMUNITYCARE_DEMO_PASSWORD.'
}

$taskLogin = Invoke-RestMethod `
    -Method Post `
    -Uri "$($ApiBaseUrl.TrimEnd('/'))/api/v1/auth/login" `
    -ContentType 'application/json' `
    -Body (@{ username = 'admin.demo'; password = $AdminPassword } | ConvertTo-Json)
$taskHeaders = @{
    Authorization = "Bearer $($taskLogin.accessToken)"
    'X-Confirm-Demo-Reset' = 'RESET-20'
}
$taskReset = Invoke-RestMethod `
    -Method Post `
    -Uri "$($ApiBaseUrl.TrimEnd('/'))/api/v1/demo/reset" `
    -Headers $taskHeaders
$taskReadback = Invoke-RestMethod `
    -Method Get `
    -Uri "$($ApiBaseUrl.TrimEnd('/'))/api/v1/elders" `
    -Headers @{ Authorization = "Bearer $($taskLogin.accessToken)" }
if (@($taskReadback).Count -ne 20 -or $taskReset.elderCount -ne 20) {
    throw "Reset readback failed: API returned $($taskReset.elderCount), list returned $(@($taskReadback).Count)."
}
Write-Output "Reset verified: 20 synthetic elder profiles; elapsed $($taskReset.elapsedMilliseconds) ms."
