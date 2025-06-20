for ($i=1; $i -le 20; $i++) {
    $content = "This is test document number $i for bulk upload testing.`n`nContent: Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.`n`nDocument ID: $i`nTimestamp: $(Get-Date)"
    $content | Out-File -FilePath "bulk_test_$i.md" -Encoding UTF8
    Write-Host "Created bulk_test_$i.md"
}
Write-Host "Created 20 test files for bulk upload testing"