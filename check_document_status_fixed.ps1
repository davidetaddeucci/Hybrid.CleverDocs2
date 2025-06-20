# PowerShell script to check document processing status
$env:PGPASSWORD = "MiaPassword123"

try {
    Write-Host "Checking document processing status..." -ForegroundColor Cyan
    
    # Get recent documents with their status
    Write-Host "Recent heavy test documents:" -ForegroundColor Green
    psql -h 192.168.1.4 -p 5433 -U admin -d cleverdocs -c 'SELECT "FileName", "Status", "ProgressPercentage", "CreatedAt" FROM "Documents" WHERE "FileName" LIKE ''heavy_test_document_%'' ORDER BY "CreatedAt" DESC LIMIT 20;'
    
    Write-Host "`nStatus Distribution:" -ForegroundColor Yellow
    psql -h 192.168.1.4 -p 5433 -U admin -d cleverdocs -c 'SELECT "Status", COUNT(*) as count FROM "Documents" WHERE "FileName" LIKE ''heavy_test_document_%'' GROUP BY "Status" ORDER BY "Status";'
    
} catch {
    Write-Error "Error checking document status: $($_.Exception.Message)"
}
