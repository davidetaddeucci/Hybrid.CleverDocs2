# PowerShell script to check R2R document processing status
$env:PGPASSWORD = "MiaPassword123"

try {
    Write-Host "Checking R2R document processing status for Test Collection Roberto..." -ForegroundColor Cyan
    
    # Get collection and document status
    $result = psql -h 192.168.1.4 -p 5433 -U admin -d cleverdocs -c "
        SELECT 
            c.\"Name\" as collection_name,
            COUNT(*) as total_documents,
            COUNT(d.\"R2RDocumentId\") as documents_with_r2r_id,
            COUNT(CASE WHEN d.\"R2RDocumentId\" IS NOT NULL AND d.\"R2RDocumentId\" NOT LIKE 'pending_%' THEN 1 END) as documents_with_valid_r2r_id,
            COUNT(CASE WHEN d.\"Status\" = 3 THEN 1 END) as ready_documents,
            COUNT(CASE WHEN d.\"Status\" = 1 THEN 1 END) as processing_documents,
            COUNT(CASE WHEN d.\"Status\" = 4 THEN 1 END) as failed_documents
        FROM \"Collections\" c
        LEFT JOIN \"Documents\" d ON c.\"Id\" = d.\"CollectionId\"
        WHERE c.\"Name\" LIKE '%Test%Roberto%' OR c.\"Name\" LIKE '%Test Collection Roberto%'
        GROUP BY c.\"Id\", c.\"Name\"
        ORDER BY c.\"Name\";
    "
    
    Write-Host "Collection Status:" -ForegroundColor Green
    Write-Host $result
    
    Write-Host "`nDetailed document analysis:" -ForegroundColor Cyan
    
    # Get detailed document info
    $detailResult = psql -h 192.168.1.4 -p 5433 -U admin -d cleverdocs -c "
        SELECT 
            d.\"FileName\",
            d.\"Status\",
            CASE 
                WHEN d.\"R2RDocumentId\" IS NULL THEN 'NO R2R ID'
                WHEN d.\"R2RDocumentId\" LIKE 'pending_%' THEN 'PENDING R2R'
                ELSE 'VALID R2R ID'
            END as r2r_status,
            d.\"R2RDocumentId\",
            d.\"R2RProcessedAt\",
            d.\"CreatedAt\"
        FROM \"Collections\" c
        JOIN \"Documents\" d ON c.\"Id\" = d.\"CollectionId\"
        WHERE c.\"Name\" LIKE '%Test%Roberto%' OR c.\"Name\" LIKE '%Test Collection Roberto%'
        ORDER BY d.\"CreatedAt\" DESC
        LIMIT 10;
    "
    
    Write-Host "Recent Documents:" -ForegroundColor Green
    Write-Host $detailResult
    
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}
