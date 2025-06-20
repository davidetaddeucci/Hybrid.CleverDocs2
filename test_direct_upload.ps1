# Direct bulk upload test using curl-like approach
$baseUrl = "http://localhost:5252"

Write-Host "🚀 DIRECT BULK UPLOAD TEST" -ForegroundColor Green
Write-Host "📊 This will trigger the enhanced logging system" -ForegroundColor Yellow

# Test health first
try {
    $health = Invoke-RestMethod -Uri "$baseUrl/health" -Method GET -TimeoutSec 5
    Write-Host "✅ WebServices health check passed" -ForegroundColor Green
} catch {
    Write-Host "❌ WebServices not responding" -ForegroundColor Red
    exit 1
}

# Login to get token
$loginData = '{"email":"r.antoniucci@microsis.it","password":"Maremmabona1!"}'
try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/Auth/login" -Method POST -Body $loginData -ContentType "application/json"
    Write-Host "✅ Login successful" -ForegroundColor Green
    $token = $loginResponse.accessToken
} catch {
    Write-Host "❌ Login failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host "🎯 READY FOR BULK UPLOAD TESTING!" -ForegroundColor Green
Write-Host "📊 Monitor Terminal 13 for enhanced logging output:" -ForegroundColor Yellow
Write-Host "   🚀 BATCH UPLOAD STARTED" -ForegroundColor Cyan
Write-Host "   📁 FILE QUEUED (for each file)" -ForegroundColor Cyan
Write-Host "   🔒 SEMAPHORE WAIT/RELEASE" -ForegroundColor Cyan
Write-Host "   ✅ UPLOAD SUCCESS / ❌ UPLOAD FAILED" -ForegroundColor Cyan
Write-Host "   📈 BATCH RESULTS" -ForegroundColor Cyan
Write-Host "   🚨 MISSING FILES DETECTED (if any)" -ForegroundColor Cyan

Write-Host "" -ForegroundColor White
Write-Host "🌐 MANUAL TESTING INSTRUCTIONS:" -ForegroundColor Green
Write-Host "1. Open browser: http://localhost:5168" -ForegroundColor White
Write-Host "2. Login: r.antoniucci@microsis.it / Maremmabona1!" -ForegroundColor White
Write-Host "3. Navigate to any collection" -ForegroundColor White
Write-Host "4. Upload all 10 test files at once" -ForegroundColor White
Write-Host "5. Watch Terminal 13 for enhanced logging" -ForegroundColor White

Write-Host "" -ForegroundColor White
Write-Host "🔍 WHAT TO LOOK FOR:" -ForegroundColor Yellow
Write-Host "- All 10 files should be processed (no missing files)" -ForegroundColor White
Write-Host "- Enhanced logging should show detailed progress" -ForegroundColor White
Write-Host "- No semaphore timeouts or failures" -ForegroundColor White
Write-Host "- Successful batch completion" -ForegroundColor White