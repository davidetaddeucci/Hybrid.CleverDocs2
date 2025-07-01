# Entity Framework Models Database Schema Validation Plan

## Complete List of Entity Framework Models to Validate

### Core Entities (8 models)
1. **Company** - Companies table
2. **User** - Users table  
3. **Document** - Documents table
4. **Collection** - Collections table
5. **CollectionDocument** - CollectionDocuments table
6. **DocumentChunk** - DocumentChunks table
7. **IngestionJob** - IngestionJobs table
8. **AuditLog** - AuditLogs table

### Auth Entities (2 models)
9. **TokenBlacklist** - TokenBlacklists table
10. **RefreshToken** - RefreshTokens table

### Dashboard Widgets (2 models)
11. **UserDashboardWidget** - UserDashboardWidgets table
12. **WidgetTemplate** - WidgetTemplates table

### Chat Entities (2 models)
13. **Conversation** - Conversations table
14. **Message** - Messages table

## Validation Process for Each Model

For each Entity Framework model, we will:

1. **Extract C# Class Schema**
   - Get all properties with data types
   - Get all data annotations ([Required], [MaxLength], etc.)
   - Get all Entity Framework configurations from ApplicationDbContext
   - Get all indexes and constraints

2. **Query PostgreSQL Table Schema**
   - Connect to PostgreSQL database (192.168.1.4:5433, cleverdocs)
   - Get table structure with \d "TableName"
   - Get all columns with data types, constraints, defaults
   - Get all indexes and foreign keys

3. **Compare and Validate**
   - Match every C# property to PostgreSQL column
   - Verify data types are compatible
   - Verify nullable/required settings match
   - Verify string length constraints match
   - Verify indexes and foreign keys match
   - Document any mismatches

4. **Generate Fix Scripts**
   - Create SQL scripts to fix any schema mismatches
   - Execute scripts to align database with Entity Framework models

## Known Issues Already Identified
- **TokenBlacklists**: Missing UserId column (CRITICAL - causes authentication failures)
- **TokenBlacklists**: Missing Reason column (FIXED)

## Validation Status
- [ ] Company
- [ ] User  
- [ ] Document
- [ ] Collection
- [ ] CollectionDocument
- [ ] DocumentChunk
- [ ] IngestionJob
- [ ] AuditLog
- [ ] TokenBlacklist (CRITICAL - needs immediate fix)
- [ ] RefreshToken
- [ ] UserDashboardWidget
- [ ] WidgetTemplate
- [ ] Conversation
- [ ] Message

## Database Connection Details
- Host: 192.168.1.4
- Port: 5433
- Database: cleverdocs
- User: admin
- Password: Florealia2025!
