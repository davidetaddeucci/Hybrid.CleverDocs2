# Test Document 1

## Overview
This is a test document for the Hybrid.CleverDocs2 end-to-end testing session.

## Purpose
This document is used to test:
- Document upload functionality
- Document DELETE operations
- Document VIEW operations  
- Document DOWNLOAD operations
- R2R integration and processing
- SignalR real-time updates

## Content
This document contains sample content to verify that the document processing pipeline works correctly from WebUI → WebServices → R2R → Database → WebUI.

### Features to Test
1. **Upload Process**
   - File validation
   - Progress tracking
   - R2R ingestion
   - Database storage

2. **Document Management**
   - View document details
   - Download document
   - Delete document
   - Real-time status updates

3. **Integration Verification**
   - PostgreSQL database updates
   - R2R system synchronization
   - Cache invalidation
   - SignalR notifications

## Test Data
- Document Type: Markdown (.md)
- File Size: Small (< 1KB)
- Content: Technical documentation
- Tags: testing, e2e, document-management
- Collection: Heavy Bulk Upload Test Collection

## Expected Results
- Successful upload with progress tracking
- R2R document ID assignment
- Database record creation
- Real-time status updates via SignalR
- Proper document operations (view, download, delete)
