# Test script to verify bulk upload functionality
# This script tests the bulk upload API directly

Write-Host "Testing Bulk Upload Functionality" -ForegroundColor Cyan

# Test credentials (from memory)
$loginUrl = "http://localhost:5169/auth/login"
$bulkUploadUrl = "http://localhost:5169/Documents/BulkUpload"

# Login credentials
$loginData = @{
    Email = "info@hybrid.it"
    Password = "Florealia2025!"
}

Write-Host "Step 1: Logging in..." -ForegroundColor Yellow

# Create session to maintain cookies
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession

try {
    # Get login page first to get anti-forgery token
    $loginPage = Invoke-WebRequest -Uri $loginUrl -SessionVariable session -UseBasicParsing
    
    # Extract anti-forgery token
    $tokenPattern = 'name="__RequestVerificationToken"[^>]*value="([^"]*)"'
    $tokenMatch = [regex]::Match($loginPage.Content, $tokenPattern)
    if ($tokenMatch.Success) {
        $token = $tokenMatch.Groups[1].Value
        Write-Host "Anti-forgery token obtained" -ForegroundColor Green
    } else {
        Write-Host "Failed to extract anti-forgery token" -ForegroundColor Red
        exit 1
    }

    # Perform login
    $loginBody = @{
        Email = $loginData.Email
        Password = $loginData.Password
        __RequestVerificationToken = $token
    }

    $loginResponse = Invoke-WebRequest -Uri $loginUrl -Method POST -Body $loginBody -WebSession $session -UseBasicParsing
    
    if ($loginResponse.StatusCode -eq 200 -or $loginResponse.Headers.Location) {
        Write-Host "✅ Login successful" -ForegroundColor Green
    } else {
        Write-Host "❌ Login failed" -ForegroundColor Red
        exit 1
    }

    Write-Host "Step 2: Preparing test files..." -ForegroundColor Yellow

    # Create multipart form data for bulk upload
    $boundary = [System.Guid]::NewGuid().ToString()
    $LF = "`r`n"
    
    # Get new anti-forgery token for upload
    $uploadPage = Invoke-WebRequest -Uri "http://localhost:5169/documents/upload" -WebSession $session -UseBasicParsing
    $uploadTokenPattern = 'name="__RequestVerificationToken"[^>]*value="([^"]*)"'
    $uploadTokenMatch = [regex]::Match($uploadPage.Content, $uploadTokenPattern)
    if ($uploadTokenMatch.Success) {
        $uploadToken = $uploadTokenMatch.Groups[1].Value
        Write-Host "Upload anti-forgery token obtained" -ForegroundColor Green
    } else {
        Write-Host "Failed to extract upload anti-forgery token" -ForegroundColor Red
        exit 1
    }

    # Build multipart form data
    $bodyLines = @()
    
    # Add anti-forgery token
    $bodyLines += "--$boundary"
    $bodyLines += "Content-Disposition: form-data; name=`"__RequestVerificationToken`""
    $bodyLines += ""
    $bodyLines += $uploadToken
    
    # Add files
    $testFiles = @("test_bulk_upload_1.md", "test_bulk_upload_2.md", "test_bulk_upload_3.md")
    
    foreach ($file in $testFiles) {
        if (Test-Path $file) {
            $fileContent = Get-Content $file -Raw
            $bodyLines += "--$boundary"
            $bodyLines += "Content-Disposition: form-data; name=`"Files`"; filename=`"$file`""
            $bodyLines += "Content-Type: text/markdown"
            $bodyLines += ""
            $bodyLines += $fileContent
        } else {
            Write-Host "⚠️ File not found: $file" -ForegroundColor Yellow
        }
    }
    
    $bodyLines += "--$boundary--"
    $body = $bodyLines -join $LF

    Write-Host "Step 3: Testing bulk upload..." -ForegroundColor Yellow

    # Perform bulk upload
    $headers = @{
        "Content-Type" = "multipart/form-data; boundary=$boundary"
    }

    $uploadResponse = Invoke-WebRequest -Uri $bulkUploadUrl -Method POST -Body $body -Headers $headers -WebSession $session -UseBasicParsing

    Write-Host "Upload Response Status: $($uploadResponse.StatusCode)" -ForegroundColor Cyan
    Write-Host "Upload Response Content: $($uploadResponse.Content)" -ForegroundColor Cyan

    if ($uploadResponse.StatusCode -eq 200) {
        Write-Host "Bulk upload test completed successfully!" -ForegroundColor Green
    } else {
        Write-Host "Bulk upload test failed" -ForegroundColor Red
    }

} catch {
    Write-Host "Error during testing: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Stack trace: $($_.ScriptStackTrace)" -ForegroundColor Red
}

Write-Host "Test completed" -ForegroundColor Cyan
