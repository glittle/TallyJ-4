# End-to-End Integration Test Script
# Tests all critical workflows of the TallyJ4 application

$baseUrl = "http://localhost:5016/api"
$testUser = "e2etest_$(Get-Random)"
$testEmail = "$testUser@test.com"
$testPassword = "Test1234!"

Write-Host "=== TallyJ4 End-to-End Integration Tests ===" -ForegroundColor Cyan
Write-Host ""

# Test 1: User Registration
Write-Host "[1/8] Testing User Registration..." -ForegroundColor Yellow
try {
    $registerBody = @{
        username = $testUser
        email = $testEmail
        password = $testPassword
        confirmPassword = $testPassword
    } | ConvertTo-Json

    $response = Invoke-RestMethod -Uri "$baseUrl/auth/register" -Method Post -Body $registerBody -ContentType "application/json"
    Write-Host "✓ User registration successful" -ForegroundColor Green
} catch {
    Write-Host "✗ User registration failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Test 2: User Login
Write-Host "[2/8] Testing User Login..." -ForegroundColor Yellow
try {
    $loginBody = @{
        username = $testUser
        password = $testPassword
    } | ConvertTo-Json

    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.token
    $headers = @{
        "Authorization" = "Bearer $token"
        "Content-Type" = "application/json"
    }
    Write-Host "✓ User login successful (Token: $($token.Substring(0,20))...)" -ForegroundColor Green
} catch {
    Write-Host "✗ User login failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Test 3: Create Election
Write-Host "[3/8] Testing Create Election..." -ForegroundColor Yellow
try {
    $electionBody = @{
        name = "E2E Test Election $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
        numberOfWinners = 9
        numberOfExtra = 2
        electionType = "Normal"
        electionMode = "Normal"
        useOnlyBallots = $true
    } | ConvertTo-Json

    $electionResponse = Invoke-RestMethod -Uri "$baseUrl/elections" -Method Post -Body $electionBody -Headers $headers
    $electionId = $electionResponse.id
    Write-Host "✓ Election created (ID: $electionId)" -ForegroundColor Green
} catch {
    Write-Host "✗ Create election failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Test 4: Get Elections List
Write-Host "[4/8] Testing Get Elections..." -ForegroundColor Yellow
try {
    $electionsUrl = "$baseUrl/elections?pageNumber=1&pageSize=10"
    $electionsResponse = Invoke-RestMethod -Uri $electionsUrl -Method Get -Headers $headers
    $electionCount = $electionsResponse.items.Count
    Write-Host "✓ Retrieved $electionCount election(s)" -ForegroundColor Green
} catch {
    Write-Host "✗ Get elections failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Test 5: Add People to Election
Write-Host "[5/8] Testing Add People..." -ForegroundColor Yellow
$peopleIds = @()
try {
    for ($i = 1; $i -le 5; $i++) {
        $personBody = @{
            electionId = $electionId
            firstName = "Person"
            lastName = "$i"
            canReceiveVotes = $true
            canVote = $true
            area = "Area1"
        } | ConvertTo-Json

        $personResponse = Invoke-RestMethod -Uri "$baseUrl/people" -Method Post -Body $personBody -Headers $headers
        $peopleIds += $personResponse.id
    }
    Write-Host "✓ Added $($peopleIds.Count) people to election" -ForegroundColor Green
} catch {
    Write-Host "✗ Add people failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Test 6: Create Ballots with Votes
Write-Host "[6/8] Testing Create Ballots and Votes..." -ForegroundColor Yellow
try {
    for ($b = 1; $b -le 3; $b++) {
        $ballotBody = @{
            electionId = $electionId
            code = "BALLOT$b"
        } | ConvertTo-Json

        $ballotResponse = Invoke-RestMethod -Uri "$baseUrl/ballots" -Method Post -Body $ballotBody -Headers $headers
        $ballotId = $ballotResponse.id

        # Add votes to ballot
        for ($v = 0; $v -lt $peopleIds.Count; $v++) {
            $voteBody = @{
                ballotId = $ballotId
                personId = $peopleIds[$v]
                rank = $v + 1
            } | ConvertTo-Json

            Invoke-RestMethod -Uri "$baseUrl/votes" -Method Post -Body $voteBody -Headers $headers | Out-Null
        }
    }
    Write-Host "✓ Created 3 ballots with votes" -ForegroundColor Green
} catch {
    Write-Host "✗ Create ballots/votes failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Test 7: Calculate Tally
Write-Host "[7/8] Testing Tally Calculation..." -ForegroundColor Yellow
try {
    $tallyResponse = Invoke-RestMethod -Uri "$baseUrl/tally/$electionId/calculate" -Method Post -Headers $headers
    Write-Host "✓ Tally calculation completed (Status: $($tallyResponse.status))" -ForegroundColor Green
} catch {
    Write-Host "✗ Tally calculation failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Test 8: Get Results
Write-Host "[8/8] Testing Get Results..." -ForegroundColor Yellow
try {
    $resultsResponse = Invoke-RestMethod -Uri "$baseUrl/results/$electionId" -Method Get -Headers $headers
    $electedCount = ($resultsResponse.items | Where-Object { $_.section -eq "Elected" }).Count
    $extraCount = ($resultsResponse.items | Where-Object { $_.section -eq "Extra" }).Count
    Write-Host "✓ Results retrieved (Elected: $electedCount, Extra: $extraCount)" -ForegroundColor Green
} catch {
    Write-Host "✗ Get results failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "=== All End-to-End Tests PASSED ✓ ===" -ForegroundColor Green
Write-Host ""
Write-Host "Test Summary:" -ForegroundColor Cyan
Write-Host "  - User: $testUser"
Write-Host "  - Election ID: $electionId"
Write-Host "  - People Added: $($peopleIds.Count)"
Write-Host "  - Ballots Created: 3"
Write-Host "  - Results: $electedCount elected, $extraCount extra"
