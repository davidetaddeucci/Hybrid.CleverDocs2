# Test Document Retry API
Write-Host "=== Testing Document Retry API ===" -ForegroundColor Green

$headers = @{
    'Content-Type' = 'application/json'
    'User-Agent' = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36'
}

# Login credentials
$loginData = @{
    email = 'r.antoniucci@microsis.it'
    password = 'Maremmabona1!'
} | ConvertTo-Json

Write-Host "Step 1: Attempting login..." -ForegroundColor Yellow
try {
    $loginResponse = Invoke-RestMethod -Uri 'http://localhost:5252/api/auth/login' -Method POST -Body $loginData -Headers $headers -SessionVariable session
    Write-Host "✅ Login successful!" -ForegroundColor Green
    Write-Host "User: $($loginResponse.user.email)" -ForegroundColor Cyan
    Write-Host "Token received: $($loginResponse.token.Length) characters" -ForegroundColor Cyan
    
    # Add Authorization header with the token
    $headers['Authorization'] = "Bearer $($loginResponse.token)"
    
    Write-Host "`nStep 2: Testing retry endpoint..." -ForegroundColor Yellow
    $documentId = '674c24b7-d01b-4316-bce0-dc8c83761caf'
    $retryUrl = "http://localhost:5252/api/UserDocuments/$documentId/retry"
    
    $retryResponse = Invoke-RestMethod -Uri $retryUrl -Method POST -Headers $headers -WebSession $session
    Write-Host "✅ Retry API call successful!" -ForegroundColor Green
    Write-Host "Response: $($retryResponse | ConvertTo-Json -Depth 3)" -ForegroundColor Cyan
    
} catch {
    Write-Host "❌ Error occurred:" -ForegroundColor Red
    Write-Host "Message: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        Write-Host "Status Code: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
        Write-Host "Status Description: $($_.Exception.Response.StatusDescription)" -ForegroundColor Red
    }
}

Write-Host "`n=== Test Complete ===" -ForegroundColor Green
