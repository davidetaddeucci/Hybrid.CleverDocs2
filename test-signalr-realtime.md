# SignalR Real-Time Test Document

## Purpose
This document is specifically created to test SignalR real-time status updates.

## Expected Behavior
1. **Upload**: Document should appear immediately with "Processing" status
2. **SignalR Update**: Status should change to "Ready" automatically via SignalR
3. **No Page Refresh**: Updates should happen without refreshing the page

## Test Details
- **Document ID**: Will be generated on upload
- **Collection**: Davide1 (231d95ba-1377-4e2d-ab57-f20bc8e8a532)
- **Expected Status Flow**: Processing → Ready
- **SignalR Event**: R2RProcessingUpdate

This test validates SignalR real-time document status updates.
