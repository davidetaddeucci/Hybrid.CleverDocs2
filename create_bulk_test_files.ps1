# Create 20 test files for bulk upload testing
Write-Host "Creating 20 test files for bulk upload testing..." -ForegroundColor Green

for ($i = 1; $i -le 20; $i++) {
    $fileName = "bulk_test_$i.md"
    $content = @"
# Bulk Upload Test Document $i

This is a test document for massive upload testing of the Hybrid.CleverDocs2 system.

## Document Information
- Document Number: $i
- Created: $(Get-Date)
- Purpose: Testing R2R rate limiting and SignalR real-time updates
- Collection: ANDREA_COLLECTION1

## Test Content
This document contains test content to verify:
1. Correct queuing system to avoid R2R rate limit exceeded errors
2. SignalR real-time status updates in the Collection documents grid
3. Proper handling of bulk document uploads (20 files simultaneously)
4. Rate limiting compliance with R2R API (10 req/s document ingestion)
5. Real-time UI updates without page refresh

## Technical Details
- File Format: Markdown (.md)
- Expected Processing: R2R document ingestion and embedding
- Expected Status Flow: Queued → Processing → Completed
- SignalR Hub: DocumentUploadHub
- Real-time Updates: Collection documents grid

## Content for Processing
Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.

Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.

### Additional Test Content
This section provides additional content for R2R processing and embedding generation. The content should be sufficient to test the document processing pipeline and verify that all components work correctly under load.

Test document $i completed successfully.
"@
    
    Set-Content -Path $fileName -Value $content -Encoding UTF8
    Write-Host "Created: $fileName" -ForegroundColor Cyan
}

Write-Host "`n✅ Successfully created 20 test files for bulk upload testing" -ForegroundColor Green
Get-ChildItem bulk_test_*.md | Select-Object Name, Length
