# Manual Testing Script for Results API
$baseUrl = "http://localhost:5020"

# Step 1: Login
Write-Host "=== Step 1: Login as admin ===" -ForegroundColor Cyan
$loginBody = @{
    email = "admin@tallyj.test"
    password = "TestPass123!"
} | ConvertTo-Json

$loginResponse = Invoke-RestMethod -Uri "$baseUrl/auth/login?useCookies=false" -Method Post -Body $loginBody -ContentType "application/json"
$token = $loginResponse.accessToken
Write-Host "✓ Login successful. Token obtained." -ForegroundColor Green
Write-Host "Token: $($token.Substring(0, 50))..." -ForegroundColor Gray
Write-Host ""

# Step 2: Get Elections
Write-Host "=== Step 2: Get all elections ===" -ForegroundColor Cyan
$headers = @{
    Authorization = "Bearer $token"
}
$elections = Invoke-RestMethod -Uri "$baseUrl/api/elections" -Method Get -Headers $headers
Write-Host "✓ Found $($elections.Count) elections" -ForegroundColor Green

if ($elections.Count -gt 0) {
    $electionGuid = $elections[0].electionGuid
    $electionName = $elections[0].name
    Write-Host "Using election: $electionName ($electionGuid)" -ForegroundColor Yellow
    Write-Host ""
    
    # Step 3: Calculate Tally
    Write-Host "=== Step 3: POST /api/results/election/$electionGuid/calculate ===" -ForegroundColor Cyan
    try {
        $calculateResponse = Invoke-RestMethod -Uri "$baseUrl/api/results/election/$electionGuid/calculate" -Method Post -Headers $headers
        Write-Host "✓ Calculate successful" -ForegroundColor Green
        Write-Host "Results summary:" -ForegroundColor Gray
        Write-Host "  - Total ballots: $($calculateResponse.statistics.totalBallots)" -ForegroundColor Gray
        Write-Host "  - Total votes: $($calculateResponse.statistics.totalVotes)" -ForegroundColor Gray
        Write-Host "  - Candidates: $($calculateResponse.results.Count)" -ForegroundColor Gray
        Write-Host ""
    } catch {
        Write-Host "✗ Calculate failed: $_" -ForegroundColor Red
    }
    
    # Step 4: Get Results
    Write-Host "=== Step 4: GET /api/results/election/$electionGuid ===" -ForegroundColor Cyan
    try {
        $results = Invoke-RestMethod -Uri "$baseUrl/api/results/election/$electionGuid" -Method Get -Headers $headers
        Write-Host "✓ Get results successful" -ForegroundColor Green
        Write-Host "Candidates in results: $($results.results.Count)" -ForegroundColor Gray
        Write-Host ""
    } catch {
        Write-Host "✗ Get results failed: $_" -ForegroundColor Red
    }
    
    # Step 5: Get Summary
    Write-Host "=== Step 5: GET /api/results/election/$electionGuid/summary ===" -ForegroundColor Cyan
    try {
        $summary = Invoke-RestMethod -Uri "$baseUrl/api/results/election/$electionGuid/summary" -Method Get -Headers $headers
        Write-Host "✓ Get summary successful" -ForegroundColor Green
        Write-Host "Statistics:" -ForegroundColor Gray
        Write-Host "  - Total ballots: $($summary.totalBallots)" -ForegroundColor Gray
        Write-Host "  - Total votes: $($summary.totalVotes)" -ForegroundColor Gray
        Write-Host "  - OK ballots: $($summary.okBallots)" -ForegroundColor Gray
        Write-Host ""
    } catch {
        Write-Host "✗ Get summary failed: $_" -ForegroundColor Red
    }
    
    # Step 6: Get Final Results
    Write-Host "=== Step 6: GET /api/results/election/$electionGuid/final ===" -ForegroundColor Cyan
    try {
        $final = Invoke-RestMethod -Uri "$baseUrl/api/results/election/$electionGuid/final" -Method Get -Headers $headers
        Write-Host "✓ Get final results successful" -ForegroundColor Green
        Write-Host "Final candidates (Elected + Extra): $($final.results.Count)" -ForegroundColor Gray
        
        # Show section breakdown
        $elected = ($final.results | Where-Object { $_.section -eq 'E' }).Count
        $extra = ($final.results | Where-Object { $_.section -eq 'X' }).Count
        Write-Host "  - Elected (E): $elected" -ForegroundColor Gray
        Write-Host "  - Extra (X): $extra" -ForegroundColor Gray
        Write-Host ""
    } catch {
        Write-Host "✗ Get final results failed: $_" -ForegroundColor Red
    }
    
    # Step 7: Test tie detection (if any)
    Write-Host "=== Step 7: Check for ties ===" -ForegroundColor Cyan
    if ($calculateResponse.ties -and $calculateResponse.ties.Count -gt 0) {
        Write-Host "✓ Ties detected: $($calculateResponse.ties.Count)" -ForegroundColor Green
        foreach ($tie in $calculateResponse.ties) {
            Write-Host "  - Group $($tie.tieBreakGroup): $($tie.numInTie) candidates tied, TieBreakRequired=$($tie.tieBreakRequired)" -ForegroundColor Gray
        }
    } else {
        Write-Host "No ties detected in this election" -ForegroundColor Yellow
    }
    Write-Host ""
    
} else {
    Write-Host "✗ No elections found in database" -ForegroundColor Red
}

# Step 8: Test without authentication
Write-Host "=== Step 8: Test unauthorized access ===" -ForegroundColor Cyan
try {
    $unauth = Invoke-RestMethod -Uri "$baseUrl/api/results/election/00000000-0000-0000-0000-000000000000/calculate" -Method Post
    Write-Host "✗ Should have returned 401, but succeeded" -ForegroundColor Red
} catch {
    if ($_.Exception.Response.StatusCode -eq 401) {
        Write-Host "✓ Correctly returned 401 Unauthorized" -ForegroundColor Green
    } else {
        Write-Host "✗ Unexpected error: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "=== Manual testing complete ===" -ForegroundColor Cyan
