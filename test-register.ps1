$body = @{
    email = 'testuser@example.com'
    password = 'Test123!'
    confirmPassword = 'Test123!'
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri 'http://localhost:5016/api/auth/register' -Method Post -Body $body -ContentType 'application/json'
    Write-Host "Registration Success!"
    Write-Host "Token:" $response.token.Substring(0, 20)...
    Write-Host "Email:" $response.email
    Write-Host "Requires2FA:" $response.requires2FA
} catch {
    Write-Host "Error:" $_.Exception.Message
    if ($_.ErrorDetails) {
        Write-Host "Response:" $_.ErrorDetails.Message
    }
}
