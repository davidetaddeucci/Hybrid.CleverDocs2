# Comprehensive Bulk Upload API Test
$baseUrl = "http://localhost:5252"

Write-Host "🚀 COMPREHENSIVE BULK UPLOAD API TEST" -ForegroundColor Green
Write-Host "📊 This will test the enhanced bulk upload system with improved configuration" -ForegroundColor Yellow
Write-Host "" -ForegroundColor White

# Step 1: Login
Write-Host "🔐 Step 1: Authenticating..." -ForegroundColor Cyan
$loginData = @{
    email = "r.antoniucci@microsis.it"
    password = "Maremmabona1!"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/Auth/login" -Method POST -Body $loginData -ContentType "application/json" -SessionVariable session
    Write-Host "✅ Authentication successful" -ForegroundColor Green
    
    # Step 2: Get collections
    Write-Host "📁 Step 2: Getting user collections..." -ForegroundColor Cyan
    $collections = Invoke-RestMethod -Uri "$baseUrl/api/UserCollections" -Method GET -WebSession $session
    Write-Host "✅ Found $($collections.Count) collections" -ForegroundColor Green
    
    if ($collections.Count -gt 0) {
        $collectionId = $collections[0].Id
        $collectionName = $collections[0].Name
        Write-Host "📂 Using collection: $collectionName (ID: $collectionId)" -ForegroundColor Yellow
        
        # Step 3: Create test files
        Write-Host "📝 Step 3: Creating test files..." -ForegroundColor Cyan
        $testFiles = @()
        for ($i = 1; $i -le 10; $i++) {
            $fileName = "api_test_$i.md"
            $content = "# Test Document $i`n`nThis is test document number $i for bulk upload API testing.`n`nContent: Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.`n`nTimestamp: " + (Get-Date)
            $content | Out-File -FilePath $fileName -Encoding UTF8
            $testFiles += $fileName
        }
        Write-Host "✅ Created $($testFiles.Count) test files" -ForegroundColor Green
        
        # Step 4: Test bulk upload API
        Write-Host "🎯 Step 4: Testing bulk upload API..." -ForegroundColor Cyan
        Write-Host "📊 Monitor Terminal 13 for enhanced logging with emojis!" -ForegroundColor Yellow
        
        # Create multipart form data
        $boundary = [System.Guid]::NewGuid().ToString()
        $LF = "`r`n"
        $bodyLines = @()
        
        # Add collection ID
        $bodyLines += "--$boundary"
        $bodyLines += "Content-Disposition: form-data; name=`"collectionId`""
        $bodyLines += ""
        $bodyLines += $collectionId
        
        # Add files
        foreach ($file in $testFiles) {
            $fileContent = Get-Content $file -Raw
            $bodyLines += "--$boundary"
            $bodyLines += "Content-Disposition: form-data; name=`"files`"; filename=`"$file`""
            $bodyLines += "Content-Type: text/markdown"
            $bodyLines += ""
            $bodyLines += $fileContent
        }
        
        $bodyLines += "--$boundary--"
        $body = $bodyLines -join $LF
        
        $headers = @{
            "Content-Type" = "multipart/form-data; boundary=$boundary"
        }
        
        Write-Host "🚀 Sending bulk upload request..." -ForegroundColor Green
        $uploadResponse = Invoke-RestMethod -Uri "$baseUrl/api/Documents/bulk-upload" -Method POST -Body $body -Headers $headers -WebSession $session
        
        Write-Host "✅ Bulk upload completed!" -ForegroundColor Green
        Write-Host "📈 Response: $($uploadResponse | ConvertTo-Json -Depth 3)" -ForegroundColor Cyan
        
        # Cleanup
        Write-Host "🧹 Cleaning up test files..." -ForegroundColor Cyan
        foreach ($file in $testFiles) {
            if (Test-Path $file) {
                Remove-Item $file -Force
            }
        }
        Write-Host "✅ Cleanup completed" -ForegroundColor Green
        
    } else {
        Write-Host "❌ No collections found - create a collection first" -ForegroundColor Red
    }
    
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        Write-Host "Status: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    }
}

Write-Host "" -ForegroundColor White
Write-Host "🏁 Bulk upload API test completed!" -ForegroundColor Cyan
Write-Host "📊 Check Terminal 13 for enhanced logging output" -ForegroundColor Yellow