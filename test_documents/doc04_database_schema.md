# Database Schema

## PostgreSQL Database Design
The system uses PostgreSQL as the primary database with a multi-tenant architecture.

### Core Tables
- **Companies**: Tenant information and configuration
- **Users**: User accounts with company association
- **Collections**: Document collections with R2R mapping
- **Documents**: Document metadata and processing status
- **Conversations**: Chat history and AI interactions

### Multi-Tenancy
- Shared database, shared schema approach
- Company-level data isolation
- Row-level security policies
- Tenant-specific R2R configurations

### Key Relationships
```sql
Companies (1) -> (N) Users
Companies (1) -> (N) Collections
Collections (1) -> (N) Documents
Users (1) -> (N) Conversations
Collections (N) -> (M) Conversations
```

### Indexing Strategy
- Primary keys on all tables
- Foreign key indexes for relationships
- Full-text search indexes on content
- Composite indexes for common queries

### Data Types
- UUIDs for primary keys
- JSONB for flexible metadata
- Timestamps with timezone
- Text for content storage

### Performance Optimizations
- Connection pooling (5-100 connections)
- Query optimization with EXPLAIN ANALYZE
- Materialized views for analytics
- Partitioning for large tables

The database design ensures scalability, performance, and data integrity for enterprise workloads.