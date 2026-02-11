# Performance Testing Script for Authentication Endpoints
# Tests authentication performance under load to ensure security improvements don't degrade performance

param(
    [string]$BaseUrl = "http://localhost:5016/api",
    [int]$ConcurrentUsers = 10,
    [int]$RequestsPerUser = 5,
    [int]$WarmupRequests = 3
)

Write-Host "=== TallyJ4 Authentication Performance Test ===" -ForegroundColor Cyan
Write-Host "Base URL: $BaseUrl" -ForegroundColor Gray
Write-Host "Concurrent Users: $ConcurrentUsers" -ForegroundColor Gray
Write-Host "Requests per User: $RequestsPerUser" -ForegroundColor Gray
Write-Host "Warmup Requests: $WarmupRequests" -ForegroundColor Gray
Write-Host ""

# Test user credentials (these should exist in the test database)
$testUsers = @(
    @{ Email = "admin@tallyj.test"; Password = "TestPass123!" },
    @{ Email = "teller@tallyj.test"; Password = "TestPass123!" },
    @{ Email = "voter@tallyj.test"; Password = "TestPass123!" }
)

# Performance metrics storage
$results = @{
    Login = @{ Times = @(); Errors = 0 }
    RefreshToken = @{ Times = @(); Errors = 0 }
    DatabaseQuery = @{ Times = @(); Errors = 0 }
}

# Function to measure request performance
function Measure-Request {
    param(
        [string]$Uri,
        [string]$Method = "POST",
        [object]$Body = $null,
        [hashtable]$Headers = @{}
    )

    $startTime = Get-Date
    try {
        $params = @{
            Uri = $Uri
            Method = $Method
            ContentType = "application/json"
        }

        if ($Body) {
            $params.Body = $Body | ConvertTo-Json
        }

        if ($Headers.Count -gt 0) {
            $params.Headers = $Headers
        }

        $response = Invoke-RestMethod @params
        $endTime = Get-Date
        $duration = ($endTime - $startTime).TotalMilliseconds

        return @{
            Success = $true
            Duration = $duration
            Response = $response
        }
    }
    catch {
        $endTime = Get-Date
        $duration = ($endTime - $startTime).TotalMilliseconds

        return @{
            Success = $false
            Duration = $duration
            Error = $_.Exception.Message
        }
    }
}

# Warmup phase
Write-Host "[WARMUP] Performing warmup requests..." -ForegroundColor Yellow
for ($i = 1; $i -le $WarmupRequests; $i++) {
    Write-Host "  Warmup request $i/$WarmupRequests..." -ForegroundColor Gray

    # Test login endpoint
    $loginBody = @{
        email = $testUsers[0].Email
        password = $testUsers[0].Password
    }

    $result = Measure-Request -Uri "$BaseUrl/auth/login" -Body $loginBody
    if (-not $result.Success) {
        Write-Host "  Warning: Warmup login failed: $($result.Error)" -ForegroundColor Yellow
    }
}
Write-Host "Warmup complete." -ForegroundColor Green
Write-Host ""

# Login Performance Test
Write-Host "[TEST 1/$($ConcurrentUsers * $RequestsPerUser)] Testing Login Performance..." -ForegroundColor Yellow

$loginJobs = @()
for ($userIndex = 0; $userIndex -lt $ConcurrentUsers; $userIndex++) {
    $user = $testUsers[$userIndex % $testUsers.Count]

    $job = Start-Job -ScriptBlock {
        param($BaseUrl, $Email, $Password, $RequestsPerUser)

        $results = @()

        for ($i = 1; $i -le $RequestsPerUser; $i++) {
            $loginBody = @{
                email = $Email
                password = $Password
            }

            $startTime = Get-Date
            try {
                $response = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body ($loginBody | ConvertTo-Json) -ContentType "application/json"
                $endTime = Get-Date
                $duration = ($endTime - $startTime).TotalMilliseconds

                $results += @{
                    Success = $true
                    Duration = $duration
                    RequestNumber = $i
                }
            }
            catch {
                $endTime = Get-Date
                $duration = ($endTime - $startTime).TotalMilliseconds

                $results += @{
                    Success = $false
                    Duration = $duration
                    Error = $_.Exception.Message
                    RequestNumber = $i
                }
            }
        }

        return $results
    } -ArgumentList $BaseUrl, $user.Email, $user.Password, $RequestsPerUser

    $loginJobs += $job
}

# Wait for all login jobs to complete
$loginJobs | Wait-Job | Out-Null

# Collect login results
foreach ($job in $loginJobs) {
    $jobResults = Receive-Job -Job $job
    foreach ($result in $jobResults) {
        $results.Login.Times += $result.Duration
        if (-not $result.Success) {
            $results.Login.Errors++
        }
    }
    Remove-Job -Job $job
}

# Calculate login statistics
$loginCount = $results.Login.Times.Count
$loginAvg = ($results.Login.Times | Measure-Object -Average).Average
$loginMin = ($results.Login.Times | Measure-Object -Minimum).Minimum
$loginMax = ($results.Login.Times | Measure-Object -Maximum).Maximum
$loginP95 = $results.Login.Times | Sort-Object | Select-Object -Skip ([math]::Floor($loginCount * 0.95)) -First 1

Write-Host "Login Performance Results:" -ForegroundColor Green
Write-Host "  Total Requests: $loginCount" -ForegroundColor White
Write-Host "  Errors: $($results.Login.Errors)" -ForegroundColor $(if ($results.Login.Errors -eq 0) { "Green" } else { "Red" })
Write-Host ("  Average: {0:N2}ms" -f $loginAvg) -ForegroundColor White
Write-Host ("  Min: {0:N2}ms" -ForegroundColor White
Write-Host ("  Max: {0:N2}ms" -f $loginMax) -ForegroundColor White
Write-Host ("  P95: {0:N2}ms" -f $loginP95) -ForegroundColor White
Write-Host ""

# Token Refresh Performance Test
Write-Host "[TEST 2] Testing Token Refresh Performance..." -ForegroundColor Yellow

# First, get some refresh tokens by logging in
$refreshTokens = @()
for ($i = 0; $i -lt [math]::Min(5, $testUsers.Count); $i++) {
    $user = $testUsers[$i]
    $loginBody = @{
        email = $user.Email
        password = $user.Password
    }

    try {
        $response = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body ($loginBody | ConvertTo-Json) -ContentType "application/json"
        if ($response.refreshToken) {
            $refreshTokens += $response.refreshToken
        }
    }
    catch {
        Write-Host "  Warning: Could not get refresh token for $($user.Email): $($_.Exception.Message)" -ForegroundColor Yellow
    }
}

if ($refreshTokens.Count -eq 0) {
    Write-Host "  ERROR: No refresh tokens available for testing" -ForegroundColor Red
    exit 1
}

Write-Host "  Obtained $($refreshTokens.Count) refresh tokens for testing" -ForegroundColor Gray

$refreshJobs = @()
for ($i = 0; $i -lt ($ConcurrentUsers * $RequestsPerUser); $i++) {
    $refreshToken = $refreshTokens[$i % $refreshTokens.Count]

    $job = Start-Job -ScriptBlock {
        param($BaseUrl, $RefreshToken)

        $refreshBody = @{
            refreshToken = $RefreshToken
        }

        $startTime = Get-Date
        try {
            $response = Invoke-RestMethod -Uri "$BaseUrl/auth/refreshToken" -Method Post -Body ($refreshBody | ConvertTo-Json) -ContentType "application/json"
            $endTime = Get-Date
            $duration = ($endTime - $startTime).TotalMilliseconds

            return @{
                Success = $true
                Duration = $duration
            }
        }
        catch {
            $endTime = Get-Date
            $duration = ($endTime - $startTime).TotalMilliseconds

            return @{
                Success = $false
                Duration = $duration
                Error = $_.Exception.Message
            }
        }
    } -ArgumentList $BaseUrl, $refreshToken

    $refreshJobs += $job
}

# Wait for refresh jobs
$refreshJobs | Wait-Job | Out-Null

# Collect refresh results
foreach ($job in $refreshJobs) {
    $result = Receive-Job -Job $job
    $results.RefreshToken.Times += $result.Duration
    if (-not $result.Success) {
        $results.RefreshToken.Errors++
    }
    Remove-Job -Job $job
}

# Calculate refresh statistics
$refreshCount = $results.RefreshToken.Times.Count
$refreshAvg = ($results.RefreshToken.Times | Measure-Object -Average).Average
$refreshMin = ($results.RefreshToken.Times | Measure-Object -Minimum).Minimum
$refreshMax = ($results.RefreshToken.Times | Measure-Object -Maximum).Maximum
$refreshP95 = $results.RefreshToken.Times | Sort-Object | Select-Object -Skip ([math]::Floor($refreshCount * 0.95)) -First 1

Write-Host "Token Refresh Performance Results:" -ForegroundColor Green
Write-Host "  Total Requests: $refreshCount" -ForegroundColor White
Write-Host "  Errors: $($results.RefreshToken.Errors)" -ForegroundColor $(if ($results.RefreshToken.Errors -eq 0) { "Green" } else { "Red" })
Write-Host ("  Average: {0:N2}ms" -f $refreshAvg) -ForegroundColor White
Write-Host ("  Min: {0:N2}ms" -ForegroundColor White
Write-Host ("  Max: {0:N2}ms" -f $refreshMax) -ForegroundColor White
Write-Host ("  P95: {0:N2}ms" -f $refreshP95) -ForegroundColor White
Write-Host ""

# Performance Assessment
Write-Host "[ANALYSIS] Performance Assessment" -ForegroundColor Cyan

$acceptableThreshold = 1000  # 1 second max acceptable response time
$warningThreshold = 500     # 500ms warning threshold

$loginIssues = 0
$refreshIssues = 0

if ($loginMax -gt $acceptableThreshold) {
    Write-Host "  WARNING: Login max response time ($([math]::Round($loginMax, 2))ms) exceeds acceptable threshold (${acceptableThreshold}ms)" -ForegroundColor Red
    $loginIssues++
}

if ($loginAvg -gt $warningThreshold) {
    Write-Host "  WARNING: Login average response time ($([math]::Round($loginAvg, 2))ms) exceeds warning threshold (${warningThreshold}ms)" -ForegroundColor Yellow
    $loginIssues++
}

if ($refreshMax -gt $acceptableThreshold) {
    Write-Host "  WARNING: Token refresh max response time ($([math]::Round($refreshMax, 2))ms) exceeds acceptable threshold (${acceptableThreshold}ms)" -ForegroundColor Red
    $refreshIssues++
}

if ($refreshAvg -gt $warningThreshold) {
    Write-Host "  WARNING: Token refresh average response time ($([math]::Round($refreshAvg, 2))ms) exceeds warning threshold (${warningThreshold}ms)" -ForegroundColor Yellow
    $refreshIssues++
}

if ($results.Login.Errors -gt 0) {
    Write-Host "  ERROR: Login had $($results.Login.Errors) errors" -ForegroundColor Red
    $loginIssues++
}

if ($results.RefreshToken.Errors -gt 0) {
    Write-Host "  ERROR: Token refresh had $($results.RefreshToken.Errors) errors" -ForegroundColor Red
    $refreshIssues++
}

# Overall assessment
$totalIssues = $loginIssues + $refreshIssues

if ($totalIssues -eq 0) {
    Write-Host ""
    Write-Host "✅ PERFORMANCE TEST PASSED" -ForegroundColor Green
    Write-Host "All authentication endpoints perform within acceptable limits."
} else {
    Write-Host ""
    Write-Host "❌ PERFORMANCE TEST FAILED" -ForegroundColor Red
    Write-Host "$totalIssues performance issues detected."
    exit 1
}

Write-Host ""
Write-Host "=== Test Summary ===" -ForegroundColor Cyan
Write-Host "Login Endpoints: $(if ($loginIssues -eq 0) { "✅ PASS" } else { "❌ FAIL" }) ($loginIssues issues)" -ForegroundColor $(if ($loginIssues -eq 0) { "Green" } else { "Red" })
Write-Host "Token Refresh: $(if ($refreshIssues -eq 0) { "✅ PASS" } else { "❌ FAIL" }) ($refreshIssues issues)" -ForegroundColor $(if ($refreshIssues -eq 0) { "Green" } else { "Red" })
Write-Host ""
Write-Host "Recommendations:" -ForegroundColor Yellow
Write-Host "- Monitor response times in production" -ForegroundColor White
Write-Host "- Consider database query optimization if response times degrade" -ForegroundColor White
Write-Host "- Ensure proper indexing on RefreshTokens table (TokenHash, ExpiresAt)" -ForegroundColor White
Write-Host "- Review encryption/decryption performance impact" -ForegroundColor White