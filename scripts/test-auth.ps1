param([string]$BaseUrl = "http://localhost:8080")

$ErrorActionPreference = 'Stop'

$testEmail = "student-$([guid]::NewGuid().ToString('N').Substring(0,10))@example.test"
$testPassword = 'TestingPassword123!'
$registration = @{
    email = $testEmail
    password = $testPassword
    firstName = 'Test'
    lastName = 'Student'
} | ConvertTo-Json

Write-Host "1. Register $testEmail"
$registered = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/auth/register" -ContentType 'application/json' -Body $registration
$registered | ConvertTo-Json

Write-Host "2. Login"
$loginBody = @{ email = $testEmail; password = $testPassword } | ConvertTo-Json
$login = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/auth/login" -ContentType 'application/json' -Body $loginBody
if (-not $login.accessToken) { throw "Login response has no access token." }
Write-Host "JWT received; expires at $($login.expiresAtUtc)"

Write-Host "3. /api/auth/me"
$me = Invoke-RestMethod -Method Get -Uri "$BaseUrl/api/auth/me" -Headers @{ Authorization = "Bearer $($login.accessToken)" }
$me | ConvertTo-Json
if ($me.email -ne $testEmail) { throw "Unexpected email in /me" }
if ($me.roles -notcontains "Student") { throw "The Student role is missing" }
Write-Host "PASS: register, login, /me, Student role" -ForegroundColor Green
