# Test Bulk Upload API
$baseUrl = "http://localhost:5252"
$loginUrl = "$baseUrl/api/Auth/login"
$uploadUrl = "$baseUrl/api/UserDocuments/batch"

# Test credentials
$loginData = @{
    email = "r.antoniucci@microsis.it"
    password = "Maremmabona1!"
} | ConvertTo-Json

Write-Host "🔐 Testing login..." -ForegroundColor Yellow

try {
    # Login to get JWT token
    $loginResponse = Invoke-RestMethod -Uri $loginUrl -Method POST -Body $loginData -ContentType "application/json" -SessionVariable session
    Write-Host "✅ Login successful" -ForegroundColor Green
    
    # Get collection ID (assuming we have a test collection)
    $collectionsUrl = "$baseUrl/api/UserCollections"
    $collections = Invoke-RestMethod -Uri $collectionsUrl -Method GET -WebSession $session
    
    if ($collections.Count -gt 0) {
        $collectionId = $collections[0].Id
        Write-Host "✅ Using collection: $($collections[0].Name) (ID: $collectionId)" -ForegroundColor Green
        
        # Prepare files for upload
        $files = @()
        for ($i = 1; $i -le 5; $i++) {  # Test with 5 files first
            $filePath = "bulk_test_$i.md"
            if (Test-Path $filePath) {
                $files += $filePath
            }
        }
        
        Write-Host "📁 Preparing to upload $($files.Count) files..." -ForegroundColor Yellow
        
        # Create multipart form data
        $boundary = [System.Guid]::NewGuid().ToString()
        $LF = "`r`n"
        
        $bodyLines = @()
        $bodyLines += "--$boundary"
        $bodyLines += "Content-Disposition: form-data; name=`"CollectionId`""
        $bodyLines += ""
        $bodyLines += $collectionId
        
        foreach ($file in $files) {
            $fileName = Split-Path $file -Leaf
            $fileContent = Get-Content $file -Raw
            
            $bodyLines += "--$boundary"
            $bodyLines += "Content-Disposition: form-data; name=`"Files`"; filename=`"$fileName`""
            $bodyLines += "Content-Type: text/markdown"
            $bodyLines += ""
            $bodyLines += $fileContent
        }
        
        $bodyLines += "--$boundary--"
        $body = $bodyLines -join $LF
        
        Write-Host "🚀 Starting bulk upload..." -ForegroundColor Yellow
        
        # Upload files
        $uploadResponse = Invoke-RestMethod -Uri $uploadUrl -Method POST -Body $body -ContentType "multipart/form-data; boundary=$boundary" -WebSession $session
        
        Write-Host "✅ Upload response received:" -ForegroundColor Green
        Write-Host ($uploadResponse | ConvertTo-Json -Depth 3)

    } else {
        Write-Host "❌ No collections found. Please create a collection first." -ForegroundColor Red
    }

} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Response: $($_.Exception.Response)" -ForegroundColor Red
}

Write-Host "🏁 Test completed" -ForegroundColor Cyan