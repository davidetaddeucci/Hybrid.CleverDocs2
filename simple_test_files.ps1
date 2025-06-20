Write-Host "Creating test files..." -ForegroundColor Green

for ($i = 1; $i -le 10; $i++) {
    $fileName = "test_upload_$i.md"
    $content = "# Test Document $i`n`nThis is test document number $i for bulk upload testing.`n`nContent: Lorem ipsum dolor sit amet.`n`nCreated: " + (Get-Date)
    $content | Out-File -FilePath $fileName -Encoding UTF8
    Write-Host "Created: $fileName" -ForegroundColor Yellow
}

Write-Host "Test files created successfully!" -ForegroundColor Green