# PowerShell script to check document processing status
$env:PGPASSWORD = "MiaPassword123"

try {
    Write-Host "Checking document processing status..." -ForegroundColor Cyan
    
    # Get recent documents with their status
    $result = psql -h 192.168.1.4 -p 5433 -U admin -d cleverdocs -c "
        SELECT 
            \"FileName\", 
            \"Status\", 
            \"ProgressPercentage\",
            \"CreatedAt\",
            \"UpdatedAt\"
        FROM \"Documents\" 
        WHERE \"FileName\" LIKE 'heavy_test_document_%' 
        ORDER BY \"CreatedAt\" DESC 
        LIMIT 20;
    "
    
    Write-Host "Document Status Results:" -ForegroundColor Green
    Write-Host $result
    
    # Get status distribution
    Write-Host "`nStatus Distribution:" -ForegroundColor Yellow
    $statusResult = psql -h 192.168.1.4 -p 5433 -U admin -d cleverdocs -c "
        SELECT 
            \"Status\", 
            COUNT(*) as count
        FROM \"Documents\" 
        WHERE \"FileName\" LIKE 'heavy_test_document_%' 
        GROUP BY \"Status\"
        ORDER BY \"Status\";
    "
    
    Write-Host $statusResult
    
} catch {
    Write-Error "Error checking document status: $($_.Exception.Message)"
}
