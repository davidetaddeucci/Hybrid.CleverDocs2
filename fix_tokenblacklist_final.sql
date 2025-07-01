-- Final fix for TokenBlacklists - Add missing UserId index

-- Add missing UserId index as per Entity Framework migrations
CREATE INDEX IF NOT EXISTS "IX_TokenBlacklists_UserId" ON "TokenBlacklists" ("UserId");

-- Verify final schema
\echo '=== FINAL TokenBlacklists SCHEMA VERIFICATION ==='
\d "TokenBlacklists"

-- Verify all indexes are present
\echo '=== ALL INDEXES VERIFICATION ==='
SELECT indexname, indexdef FROM pg_indexes WHERE tablename = 'TokenBlacklists' ORDER BY indexname;
