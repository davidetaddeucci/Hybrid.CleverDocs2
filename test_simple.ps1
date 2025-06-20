Write-Host "Testing WebServices API..." -ForegroundColor Green

try {
    $response = Invoke-RestMethod -Uri "http://localhost:5252/health" -Method GET -TimeoutSec 5
    Write-Host "Health check successful" -ForegroundColor Green
} catch {
    Write-Host "Health check failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "Test completed" -ForegroundColor Cyan