# Verify chat tables in PostgreSQL cleverdocs database
$env:PGPASSWORD = "MiaPassword123"

Write-Host "=== Verifying Conversations table ===" -ForegroundColor Green
psql -h 192.168.1.4 -p 5433 -U admin -d cleverdocs -c "\d \"Conversations\""

Write-Host "`n=== Verifying Messages table ===" -ForegroundColor Green  
psql -h 192.168.1.4 -p 5433 -U admin -d cleverdocs -c "\d \"Messages\""

Write-Host "`n=== Listing all tables ===" -ForegroundColor Green
psql -h 192.168.1.4 -p 5433 -U admin -d cleverdocs -c "\dt"
