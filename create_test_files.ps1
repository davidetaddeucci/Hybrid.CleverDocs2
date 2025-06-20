# Create test files for bulk upload testing
Write-Host "Creating test files for bulk upload..." -ForegroundColor Green

for ($i = 1; $i -le 10; $i++) {
    $fileName = "test_upload_$i.md"
    $content = @"
# Test Document $i

This is test document number $i for bulk upload testing.

## Content
Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.

## Metadata
- File: $fileName
- Created: $(Get-Date)
- Purpose: Bulk upload testing
- Test ID: $i

## Additional Content
Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.
"@
    
    $content | Out-File -FilePath $fileName -Encoding UTF8
    Write-Host "Created: $fileName" -ForegroundColor Yellow
}

Write-Host "✅ Created 10 test files for bulk upload testing" -ForegroundColor Green
Write-Host "📁 Files: test_upload_1.md to test_upload_10.md" -ForegroundColor Cyan
Write-Host "" -ForegroundColor White
Write-Host "🎯 Next steps:" -ForegroundColor Yellow
Write-Host "1. Open browser to http://localhost:5168" -ForegroundColor White
Write-Host "2. Login with r.antoniucci@microsis.it / Maremmabona1!" -ForegroundColor White
Write-Host "3. Navigate to a collection" -ForegroundColor White
Write-Host "4. Upload all 10 files at once" -ForegroundColor White
Write-Host "5. Monitor Terminal 13 for enhanced logging" -ForegroundColor White