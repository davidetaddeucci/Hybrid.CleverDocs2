# Test Document for R2RIngestionJobId

This is a test document to verify that the R2RIngestionJobId field is correctly populated in the database when documents are uploaded and processed through the R2R system.

## Test Details

- **Purpose**: Verify R2RIngestionJobId implementation
- **Date**: June 21, 2025
- **Collection**: Test210625
- **Expected Result**: Document should have both R2RDocumentId and R2RIngestionJobId populated after processing

## System Architecture

The Hybrid.CleverDocs2 system integrates with SciPhi AI R2R API for document processing. The workflow is:

1. Document uploaded via WebUI
2. Document queued for R2R processing with generated JobId
3. R2R processes the document and returns DocumentId
4. Both JobId and DocumentId are saved to PostgreSQL database

## Testing Verification

After upload, verify in PostgreSQL database:
- R2RDocumentId should be populated (UUID format)
- R2RIngestionJobId should be populated (UUID format)
- Both fields should be non-null for successful processing

This test document will help verify the complete end-to-end functionality.
