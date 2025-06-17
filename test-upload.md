# Test Document Upload

This is a test markdown document to verify that the upload system works correctly after fixing the validation issue.

## Features Tested

- File validation for .md files with `application/text` content type
- Document saving to database immediately after upload
- Cache invalidation to ensure frontend sees new documents
- R2R processing queue integration

## Expected Behavior

1. File should pass validation
2. Document should be saved to database with "Processing" status
3. Frontend should see the document immediately
4. R2R processing should happen in background
5. Document status should update when R2R completes

## Test Results

This document upload will verify that all the fixes are working correctly.