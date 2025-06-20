# Simple test to trigger bulk upload logging
$baseUrl = "http://localhost:5252"

Write-Host "🔍 Testing WebServices health..." -ForegroundColor Yellow

try {
    # Test if WebServices is responding
    $healthResponse = Invoke-RestMethod -Uri "$baseUrl/health" -Method GET -TimeoutSec 10
    Write-Host "✅ WebServices is healthy" -ForegroundColor Green
    
    # Test login endpoint
    Write-Host "🔐 Testing login endpoint..." -ForegroundColor Yellow
    $loginData = @{
        email = "r.antoniucci@microsis.it"
        password = "Maremmabona1!"
    } | ConvertTo-Json
    
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/Auth/login" -Method POST -Body $loginData -ContentType "application/json" -SessionVariable session
    Write-Host "✅ Login successful - Token received" -ForegroundColor Green
    
    # Get user collections
    Write-Host "📁 Getting user collections..." -ForegroundColor Yellow
    $collections = Invoke-RestMethod -Uri "$baseUrl/api/UserCollections" -Method GET -WebSession $session
    Write-Host "✅ Found $($collections.Count) collections" -ForegroundColor Green
    
    if ($collections.Count -gt 0) {
        $collection = $collections[0]
        Write-Host "📂 Using collection: $($collection.Name) (ID: $($collection.Id))" -ForegroundColor Cyan
        
        # Now the enhanced logging should show activity in the WebServices terminal
        Write-Host "🎯 Ready for bulk upload testing!" -ForegroundColor Green
        Write-Host "📊 Monitor the WebServices terminal for enhanced logging output" -ForegroundColor Yellow
        Write-Host "🔍 Look for emoji-based log messages starting with 🚀 BATCH UPLOAD STARTED" -ForegroundColor Yellow
    } else {
        Write-Host "❌ No collections found - create a collection first" -ForegroundColor Red
    }
    
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        Write-Host "Status: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    }
}

Write-Host "🏁 Test completed - System ready for bulk upload testing" -ForegroundColor Cyan