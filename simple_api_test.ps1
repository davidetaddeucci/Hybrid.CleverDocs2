# Simple API Test for Bulk Upload
$baseUrl = "http://localhost:5252"

Write-Host "Testing API endpoints..." -ForegroundColor Green

# Test health endpoint
try {
    $health = Invoke-RestMethod -Uri "$baseUrl/health" -Method GET -TimeoutSec 10
    Write-Host "Health check: OK" -ForegroundColor Green
} catch {
    Write-Host "Health check failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Test login
$loginData = '{"email":"r.antoniucci@microsis.it","password":"Maremmabona1!"}'
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/Auth/login" -Method POST -Body $loginData -ContentType "application/json" -SessionVariable session
    Write-Host "Login: OK" -ForegroundColor Green
} catch {
    Write-Host "Login failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Test collections
try {
    $collections = Invoke-RestMethod -Uri "$baseUrl/api/UserCollections" -Method GET -WebSession $session
    Write-Host "Collections found: $($collections.Count)" -ForegroundColor Green
    
    if ($collections.Count -gt 0) {
        Write-Host "First collection: $($collections[0].Name)" -ForegroundColor Yellow
        Write-Host "Collection ID: $($collections[0].Id)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "Collections failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "API test completed successfully!" -ForegroundColor Cyan