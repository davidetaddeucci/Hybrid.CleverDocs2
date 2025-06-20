# PowerShell script to test heavy bulk upload (20 x 2MB files)
param(
    [string]$FilesDirectory = "heavy_test_files",
    [string]$WebUIUrl = "http://localhost:5169",
    [string]$LoginEmail = "info@hybrid.it",
    [string]$LoginPassword = "Florealia2025!",
    [string]$CollectionId = $null
)

Write-Host "🚀 HEAVY BULK UPLOAD TEST - R2R Ingestion Status Verification" -ForegroundColor Cyan
Write-Host "=============================================================" -ForegroundColor Cyan

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

Write-Host "📁 Found $($testFiles.Count) test files in $FilesDirectory" -ForegroundColor Green
$totalSize = ($testFiles | Measure-Object -Property Length -Sum).Sum
$totalSizeMB = [math]::Round($totalSize / 1MB, 2)
Write-Host "📊 Total size: $totalSize bytes ($totalSizeMB MB)" -ForegroundColor Green

# Step 1: Login to get authentication
Write-Host "`n🔐 Step 1: Authenticating..." -ForegroundColor Yellow

$loginUrl = "$WebUIUrl/Auth/Login"
$loginData = @{
    Email = $LoginEmail
    Password = $LoginPassword
} | ConvertTo-Json

try {
    # Create session for cookie management
    $session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    
    # Get login page first to get anti-forgery token
    $loginPageResponse = Invoke-WebRequest -Uri $loginUrl -Method GET -SessionVariable session
    $loginPage = $loginPageResponse.Content
    
    # Extract anti-forgery token
    if ($loginPage -match '__RequestVerificationToken.*?value="([^"]+)"') {
        $antiForgeryToken = $matches[1]
        Write-Host "✅ Anti-forgery token obtained" -ForegroundColor Green
    } else {
        Write-Error "Failed to extract anti-forgery token from login page"
        exit 1
    }
    
    # Prepare login form data
    $loginFormData = @{
        Email = $LoginEmail
        Password = $LoginPassword
        __RequestVerificationToken = $antiForgeryToken
    }
    
    # Perform login
    $loginResponse = Invoke-WebRequest -Uri $loginUrl -Method POST -Body $loginFormData -SessionVariable session -ContentType "application/x-www-form-urlencoded"
    
    if ($loginResponse.StatusCode -eq 200 -and $loginResponse.BaseResponse.ResponseUri.AbsolutePath -ne "/Auth/Login") {
        Write-Host "✅ Login successful" -ForegroundColor Green
    } else {
        Write-Error "Login failed"
        exit 1
    }
} catch {
    Write-Error "Login error: $($_.Exception.Message)"
    exit 1
}

# Step 2: Get upload page and anti-forgery token
Write-Host "`n📄 Step 2: Preparing upload..." -ForegroundColor Yellow

$uploadUrl = "$WebUIUrl/Documents/Upload"
try {
    $uploadPageResponse = Invoke-WebRequest -Uri $uploadUrl -Method GET -WebSession $session
    $uploadPage = $uploadPageResponse.Content
    
    # Extract anti-forgery token for upload
    if ($uploadPage -match '__RequestVerificationToken.*?value="([^"]+)"') {
        $uploadAntiForgeryToken = $matches[1]
        Write-Host "✅ Upload anti-forgery token obtained" -ForegroundColor Green
    } else {
        Write-Error "Failed to extract anti-forgery token from upload page"
        exit 1
    }
} catch {
    Write-Error "Failed to get upload page: $($_.Exception.Message)"
    exit 1
}

# Step 3: Prepare bulk upload
Write-Host "`n🚀 Step 3: Starting heavy bulk upload test..." -ForegroundColor Yellow
Write-Host "📊 Uploading $($testFiles.Count) files (Total: $totalSizeMB MB)" -ForegroundColor Cyan

$bulkUploadUrl = "$WebUIUrl/Documents/BulkUpload"

# Create multipart form data
$boundary = [System.Guid]::NewGuid().ToString()
$LF = "`r`n"

$bodyLines = @()

# Add anti-forgery token
$bodyLines += "--$boundary"
$bodyLines += "Content-Disposition: form-data; name=`"__RequestVerificationToken`""
$bodyLines += ""
$bodyLines += $uploadAntiForgeryToken

# Add collection ID if provided
if ($CollectionId) {
    $bodyLines += "--$boundary"
    $bodyLines += "Content-Disposition: form-data; name=`"CollectionId`""
    $bodyLines += ""
    $bodyLines += $CollectionId
    Write-Host "📁 Using Collection ID: $CollectionId" -ForegroundColor Cyan
}

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
    Write-Host "📄 Adding file $fileIndex/$($testFiles.Count): $($file.Name) ($([math]::Round($file.Length / (1024*1024), 2)) MB)" -ForegroundColor Gray
    
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

Write-Host "`n🎯 Sending bulk upload request..." -ForegroundColor Yellow
Write-Host "📊 Request size: $([math]::Round($body.Length / 1MB, 2)) MB" -ForegroundColor Cyan

# Record start time
$startTime = Get-Date

try {
    # Send bulk upload request
    $uploadResponse = Invoke-WebRequest -Uri $bulkUploadUrl -Method POST -Body $body -ContentType "multipart/form-data; boundary=$boundary" -WebSession $session -TimeoutSec 300
    
    $endTime = Get-Date
    $duration = $endTime - $startTime
    
    if ($uploadResponse.StatusCode -eq 200) {
        $result = $uploadResponse.Content | ConvertFrom-Json
        
        Write-Host "`n✅ BULK UPLOAD SUCCESS!" -ForegroundColor Green
        Write-Host "📊 Upload Statistics:" -ForegroundColor Cyan
        Write-Host "   • Files uploaded: $($testFiles.Count)" -ForegroundColor White
        Write-Host "   • Total size: $totalSizeMB MB" -ForegroundColor White
        Write-Host "   • Duration: $($duration.TotalSeconds.ToString('F2')) seconds" -ForegroundColor White
        Write-Host "   • Average speed: $([math]::Round($totalSizeMB / $duration.TotalSeconds, 2)) MB/s" -ForegroundColor White
        
        if ($result.success) {
            Write-Host "`n📝 Response Details:" -ForegroundColor Cyan
            Write-Host "   • Message: $($result.message)" -ForegroundColor White
            Write-Host "   • Collection ID: $($result.collectionId)" -ForegroundColor White
            Write-Host "   • File Count: $($result.fileCount)" -ForegroundColor White
            Write-Host "   • Timestamp: $($result.timestamp)" -ForegroundColor White
            
            if ($result.collectionId) {
                Write-Host "`n🎯 Next Steps:" -ForegroundColor Yellow
                Write-Host "   1. Navigate to Collection Detail page: $WebUIUrl/Collections/$($result.collectionId)" -ForegroundColor White
                Write-Host "   2. Monitor real-time R2R ingestion status updates via SignalR" -ForegroundColor White
                Write-Host "   3. Verify status transitions: Queued → Processing → Completed" -ForegroundColor White
                Write-Host "   4. Ensure NOT all documents show 'Completed' immediately" -ForegroundColor White
            }
        } else {
            Write-Warning "Upload reported as unsuccessful: $($result.message)"
        }
    } else {
        Write-Error "Upload failed with status: $($uploadResponse.StatusCode)"
        Write-Host "Response: $($uploadResponse.Content)" -ForegroundColor Red
    }
    
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

Write-Host "`n🏁 Heavy bulk upload test completed!" -ForegroundColor Cyan
