# PowerShell script to test heavy bulk upload using the working approach
param(
    [string]$FilesDirectory = "heavy_test_files"
)

Write-Host "HEAVY BULK UPLOAD TEST - R2R Ingestion Status Verification" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# Check if files directory exists
if (!(Test-Path $FilesDirectory)) {
    Write-Error "Files directory not found: $FilesDirectory"
    exit 1
}

# Get all test files
$testFiles = Get-ChildItem -Path $FilesDirectory -Filter "*.md" | Sort-Object Name
if ($testFiles.Count -eq 0) {
    Write-Error "No test files found in directory: $FilesDirectory"
    exit 1
}

Write-Host "Found $($testFiles.Count) test files in $FilesDirectory" -ForegroundColor Green
$totalSize = ($testFiles | Measure-Object -Property Length -Sum).Sum
$totalSizeMB = [math]::Round($totalSize / 1MB, 2)
Write-Host "Total size: $totalSize bytes ($totalSizeMB MB)" -ForegroundColor Green

# Step 1: Login to get authentication
Write-Host "`nStep 1: Authenticating..." -ForegroundColor Yellow

$loginUrl = "http://localhost:5252/api/auth/login"
$loginData = @{
    email = "info@hybrid.it"
    password = "Florealia2025!"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri $loginUrl -Method POST -Body $loginData -ContentType "application/json"
    
    if ($response.accessToken) {
        Write-Host "Login successful!" -ForegroundColor Green
        $accessToken = $response.accessToken
    } else {
        Write-Error "Login failed - no access token received"
        exit 1
    }
} catch {
    Write-Error "Login error: $($_.Exception.Message)"
    exit 1
}

# Step 2: Prepare bulk upload using WebServices API directly
Write-Host "`nStep 2: Starting heavy bulk upload test..." -ForegroundColor Yellow
Write-Host "Uploading $($testFiles.Count) files (Total: $totalSizeMB MB)" -ForegroundColor Cyan

$bulkUploadUrl = "http://localhost:5252/api/DocumentUpload/batch"

# Record start time
$startTime = Get-Date

try {
    # Create multipart form data
    $boundary = [System.Guid]::NewGuid().ToString()
    $LF = "`r`n"
    $bodyLines = @()

    # Add upload options
    $bodyLines += "--$boundary"
    $bodyLines += "Content-Disposition: form-data; name=`"Options.ExtractMetadata`""
    $bodyLines += ""
    $bodyLines += "true"

    $bodyLines += "--$boundary"
    $bodyLines += "Content-Disposition: form-data; name=`"Options.PerformOCR`""
    $bodyLines += ""
    $bodyLines += "true"

    $bodyLines += "--$boundary"
    $bodyLines += "Content-Disposition: form-data; name=`"Options.MaxParallelUploads`""
    $bodyLines += ""
    $bodyLines += "10"

    # Add all files
    $fileIndex = 1
    foreach ($file in $testFiles) {
        $fileSizeMB = [math]::Round($file.Length / 1MB, 2)
        Write-Host "Adding file $fileIndex/$($testFiles.Count): $($file.Name) ($fileSizeMB MB)" -ForegroundColor Gray
        
        $bodyLines += "--$boundary"
        $bodyLines += "Content-Disposition: form-data; name=`"Files`"; filename=`"$($file.Name)`""
        $bodyLines += "Content-Type: text/markdown"
        $bodyLines += ""
        
        # Read file content
        $fileContent = [System.IO.File]::ReadAllText($file.FullName)
        $bodyLines += $fileContent
        
        $fileIndex++
    }

    # Close boundary
    $bodyLines += "--$boundary--"

    # Join all lines
    $body = $bodyLines -join $LF

    Write-Host "`nSending bulk upload request..." -ForegroundColor Yellow
    $requestSizeMB = [math]::Round($body.Length / 1MB, 2)
    Write-Host "Request size: $requestSizeMB MB" -ForegroundColor Cyan

    # Prepare headers
    $headers = @{
        "Authorization" = "Bearer $accessToken"
        "Content-Type" = "multipart/form-data; boundary=$boundary"
    }

    # Send bulk upload request
    $uploadResponse = Invoke-RestMethod -Uri $bulkUploadUrl -Method POST -Body $body -Headers $headers -TimeoutSec 300

    $endTime = Get-Date
    $duration = $endTime - $startTime

    Write-Host "`nBULK UPLOAD SUCCESS!" -ForegroundColor Green
    Write-Host "Upload Statistics:" -ForegroundColor Cyan
    Write-Host "  Files uploaded: $($testFiles.Count)" -ForegroundColor White
    Write-Host "  Total size: $totalSizeMB MB" -ForegroundColor White
    Write-Host "  Duration: $($duration.TotalSeconds.ToString('F2')) seconds" -ForegroundColor White
    Write-Host "  Average speed: $([math]::Round($totalSizeMB / $duration.TotalSeconds, 2)) MB/s" -ForegroundColor White
    
    Write-Host "`nResponse Details:" -ForegroundColor Cyan
    Write-Host "  Success: $($uploadResponse.success)" -ForegroundColor White
    Write-Host "  Message: $($uploadResponse.message)" -ForegroundColor White
    Write-Host "  Session ID: $($uploadResponse.data.sessionId)" -ForegroundColor White
    Write-Host "  Files processed: $($uploadResponse.data.files.Count)" -ForegroundColor White
    
    Write-Host "`nNext Steps:" -ForegroundColor Yellow
    Write-Host "  1. Navigate to WebUI: http://localhost:5169/Documents" -ForegroundColor White
    Write-Host "  2. Monitor real-time R2R ingestion status updates via SignalR" -ForegroundColor White
    Write-Host "  3. Verify status transitions: Queued -> Processing -> Completed" -ForegroundColor White
    Write-Host "  4. Ensure NOT all documents show 'Completed' immediately" -ForegroundColor White
    
    # Open browser to documents page for manual verification
    Write-Host "`nOpening browser to Documents page..." -ForegroundColor Yellow
    Start-Process "http://localhost:5169/Documents"
    
} catch {
    $endTime = Get-Date
    $duration = $endTime - $startTime
    
    Write-Error "Bulk upload failed after $($duration.TotalSeconds.ToString('F2')) seconds"
    Write-Error "Error: $($_.Exception.Message)"
    
    if ($_.Exception.Response) {
        $errorResponse = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($errorResponse)
        $errorContent = $reader.ReadToEnd()
        Write-Host "Error response: $errorContent" -ForegroundColor Red
    }
}

Write-Host "`nHeavy bulk upload test completed!" -ForegroundColor Cyan
