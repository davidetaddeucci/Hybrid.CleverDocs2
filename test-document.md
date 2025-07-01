# JWT Authentication Test Document

This is a test document to verify the complete document upload pipeline:

## Test Objectives
1. WebUI upload functionality
2. R2R ingestion process
3. Database updates with R2R document IDs
4. Real-time status updates via SignalR

## Document Content
This document contains sample content to test the R2R processing pipeline.

### Technical Details
- **Upload Date**: June 21, 2025
- **Collection**: JWT Authentication Test After Fresh Login
- **Purpose**: End-to-end pipeline testing

The document should be processed by R2R and receive a unique document ID that gets stored in the PostgreSQL database.

## Expected Results
- Document uploaded successfully
- R2R processing initiated
- Database record created with R2R document ID
- Real-time status updates displayed in WebUI