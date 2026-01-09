Write-Host "WARNING: This will drop and recreate the database!" -ForegroundColor Yellow
$confirm = Read-Host "Are you sure? (yes/no)"
if ($confirm -ne "yes") { 
    Write-Host "Database reset cancelled." -ForegroundColor Green
    exit 
}

Write-Host "Dropping database..." -ForegroundColor Cyan
dotnet ef database drop --project TallyJ4.csproj --force

Write-Host "Applying migrations..." -ForegroundColor Cyan
dotnet ef database update --project TallyJ4.csproj

Write-Host "" 
Write-Host "Database reset complete!" -ForegroundColor Green
Write-Host "Run 'dotnet run' to seed the database with test data." -ForegroundColor Cyan
