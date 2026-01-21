$baseUrl = "http://localhost:5020"

# Try to register a new user
Write-Host "Attempting to register new user..." -ForegroundColor Cyan

$registerBody = @{
    email = "testuser@tallyj.test"
    password = "TestPass123!"
} | ConvertTo-Json

try {
    $registerResponse = Invoke-RestMethod -Uri "$baseUrl/auth/register" -Method Post -Body $registerBody -ContentType "application/json"
    Write-Host "✓ Registration successful" -ForegroundColor Green
    
    # Try to login
    Write-Host "Attempting to login with new user..." -ForegroundColor Cyan
    $loginBody = @{
        email = "testuser@tallyj.test"
        password = "TestPass123!"
    } | ConvertTo-Json
    
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/auth/login?useCookies=false" -Method Post -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.accessToken
    Write-Host "✓ Login successful!" -ForegroundColor Green
    Write-Host "Token: $($token.Substring(0, 50))..." -ForegroundColor Gray
    
} catch {
    Write-Host "Error: $_" -ForegroundColor Red
    Write-Host "Status Code: $($_.Exception.Response.StatusCode.Value__)" -ForegroundColor Red
}
