# Simple health check test
$baseUrl = "http://localhost:5252"

Write-Host "🔍 Testing WebServices health..." -ForegroundColor Yellow

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/health" -Method GET -TimeoutSec 10
    Write-Host "✅ WebServices is healthy: $($response | ConvertTo-Json)" -ForegroundColor Green
} catch {
    Write-Host "❌ Health check failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "🏁 Health check completed" -ForegroundColor Cyan