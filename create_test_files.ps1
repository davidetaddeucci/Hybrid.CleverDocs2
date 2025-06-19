# Create test files for bulk upload testing
Write-Host "=== Creating Test Files for Bulk Upload ===" -ForegroundColor Green

# Create test files directory
$testDir = 'test_files_andrea'
if (Test-Path $testDir) { 
    Remove-Item $testDir -Recurse -Force 
    Write-Host "Removed existing test directory" -ForegroundColor Yellow
}
New-Item -ItemType Directory -Path $testDir | Out-Null
Write-Host "Created test directory: $testDir" -ForegroundColor Cyan

# Create 20 test files
Write-Host "`nCreating 20 test files..." -ForegroundColor Yellow

for ($i = 1; $i -le 20; $i++) {
    $fileName = "andrea_test_$i.txt"
    $filePath = Join-Path $testDir $fileName
    
    $content = @"
This is test document $i for rate limiting verification.

Created for testing bulk upload with R2R rate limiting.
Document ID: $i
Timestamp: $(Get-Date)
User: a.morviducci@microsis.it
Collection: ANDREA_COLLECTION1

Content for testing purposes:
- Rate limiting verification
- SignalR real-time updates
- Bulk upload processing
- R2R queue management

This document should be processed through the R2R pipeline
with proper rate limiting (10 req/s) and real-time status updates.
"@

    Set-Content -Path $filePath -Value $content -Encoding UTF8
    Write-Host "✅ Created: $fileName ($(Get-Item $filePath | Select-Object -ExpandProperty Length) bytes)" -ForegroundColor Green
}

Write-Host "`n=== Summary ===" -ForegroundColor Green
Write-Host "Successfully created 20 test files in '$testDir' directory" -ForegroundColor Cyan
Write-Host "Files ready for bulk upload testing" -ForegroundColor Cyan

# List all created files
Write-Host "`nCreated files:" -ForegroundColor Yellow
Get-ChildItem $testDir | Select-Object Name, Length | Format-Table -AutoSize
