Write-Host "Testing bulk upload endpoint..." -ForegroundColor Green

# Test if the endpoint exists
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5252/api/DocumentUpload/batch" -Method OPTIONS -TimeoutSec 5
    Write-Host "✅ Endpoint exists! Status: $($response.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "❌ Endpoint test failed: $($_.Exception.Message)" -ForegroundColor Red
    
    # Try alternative URLs
    Write-Host "Testing alternative URLs..." -ForegroundColor Yellow
    
    try {
        $response2 = Invoke-WebRequest -Uri "http://localhost:5252/api/documentupload/batch" -Method OPTIONS -TimeoutSec 5
        Write-Host "✅ Alternative URL works! Status: $($response2.StatusCode)" -ForegroundColor Green
    } catch {
        Write-Host "❌ Alternative URL failed: $($_.Exception.Message)" -ForegroundColor Red
    }
}