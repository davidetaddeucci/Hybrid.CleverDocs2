# SignalR Real-Time Communication

## Overview
SignalR provides real-time bidirectional communication between the server and clients for live updates.

## Hubs Implementation
1. **CollectionHub** (/hubs/collection): Collection updates and analytics
2. **DocumentUploadHub** (/hubs/upload): Upload progress and status updates

## Real-Time Features
- Live upload progress tracking with speed and ETA
- Document processing status updates
- Collection modification notifications
- Search suggestions and results
- Analytics and usage statistics
- Error notifications and alerts

## Connection Management
- Automatic reconnection on disconnect
- Connection state monitoring
- User-based connection grouping
- Multi-tenant message isolation

## Message Types
- Progress updates with percentage and speed
- Status changes (Queued → Processing → Ready)
- Error notifications with retry options
- Completion notifications with results
- Analytics updates for dashboards

## Performance Optimizations
- Message batching for high-frequency updates
- Connection pooling and reuse
- Efficient serialization
- Selective client targeting

## Security
- JWT-based authentication
- User context validation
- Message authorization
- Rate limiting for message sending

This document tests SignalR-related content processing.