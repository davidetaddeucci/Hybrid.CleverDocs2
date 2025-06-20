# Test Bulk Upload API with Enhanced Logging
$baseUrl = "http://localhost:5252"

Write-Host "🚀 TESTING BULK UPLOAD WITH ENHANCED LOGGING" -ForegroundColor Green
Write-Host "📊 Monitor Terminal 3 for enhanced logging output with emojis" -ForegroundColor Yellow
Write-Host "" -ForegroundColor White

# Test login
Write-Host "🔐 Step 1: Testing login..." -ForegroundColor Cyan
$loginData = @{
    email = "r.antoniucci@microsis.it"
    password = "Maremmabona1!"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/Auth/login" -Method POST -Body $loginData -ContentType "application/json" -SessionVariable session
    Write-Host "✅ Login successful" -ForegroundColor Green
    
    # Get collections
    Write-Host "📁 Step 2: Getting collections..." -ForegroundColor Cyan
    $collections = Invoke-RestMethod -Uri "$baseUrl/api/UserCollections" -Method GET -WebSession $session
    Write-Host "✅ Found $($collections.Count) collections" -ForegroundColor Green
    
    if ($collections.Count -gt 0) {
        $collectionId = $collections[0].Id
        Write-Host "📂 Using collection: $($collections[0].Name)" -ForegroundColor Yellow
        Write-Host "🆔 Collection ID: $collectionId" -ForegroundColor Yellow
        
        Write-Host "" -ForegroundColor White
        Write-Host "🎯 READY FOR BULK UPLOAD TEST!" -ForegroundColor Green
        Write-Host "📋 Next steps:" -ForegroundColor Yellow
        Write-Host "   1. Open browser to http://localhost:5168" -ForegroundColor White
        Write-Host "   2. Login with r.antoniucci@microsis.it / Maremmabona1!" -ForegroundColor White
        Write-Host "   3. Navigate to the collection: $($collections[0].Name)" -ForegroundColor White
        Write-Host "   4. Upload multiple files (bulk_test_1.md to bulk_test_20.md)" -ForegroundColor White
        Write-Host "   5. Watch Terminal 3 for enhanced logging with emojis:" -ForegroundColor White
        Write-Host "      🚀 BATCH UPLOAD STARTED" -ForegroundColor Green
        Write-Host "      📁 FILE QUEUED" -ForegroundColor Green
        Write-Host "      🔒 SEMAPHORE WAIT/RELEASE" -ForegroundColor Green
        Write-Host "      ✅ UPLOAD SUCCESS/❌ UPLOAD FAILED" -ForegroundColor Green
        Write-Host "      📈 BATCH RESULTS" -ForegroundColor Green
        
    } else {
        Write-Host "❌ No collections found" -ForegroundColor Red
    }
    
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "" -ForegroundColor White
Write-Host "🏁 Test setup completed - Ready for manual bulk upload testing" -ForegroundColor Cyan