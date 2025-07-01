# Database Schema Documentation

## PostgreSQL Multi-Tenant Architecture
The system uses a shared database with company-based tenant isolation.

## Core Tables
1. **Companies**: Tenant organizations
2. **Users**: User accounts with role-based access
3. **Collections**: Document collections with R2R sync
4. **Documents**: Document metadata and processing status
5. **Conversations**: Chat conversations with R2R integration
6. **Messages**: Chat messages and responses
7. **AuditLogs**: System activity tracking
8. **UserDashboardWidgets**: Customizable dashboard configuration

## Key Features
- Multi-tenant data isolation
- Foreign key relationships
- Audit trail for all operations
- Real-time status tracking
- JSON metadata storage
- Optimized indexes for performance

## Entity Relationships
- Companies → Users (1:N)
- Users → Collections (1:N)
- Collections → Documents (1:N)
- Users → Conversations (1:N)
- Conversations → Messages (1:N)

## Data Types
- UUIDs for tenant isolation
- JSONB for flexible metadata
- Timestamps with timezone
- Enum types for status fields

This document tests database-related content processing.