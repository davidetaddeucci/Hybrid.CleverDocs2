-- Comprehensive TokenBlacklist Schema Validation
-- Compare Entity Framework model with PostgreSQL table

-- 1. Show current table structure
\echo '=== CURRENT TokenBlacklists TABLE STRUCTURE ==='
\d "TokenBlacklists"

-- 2. Show all columns with details
\echo '=== DETAILED COLUMN INFORMATION ==='
SELECT 
    column_name,
    data_type,
    character_maximum_length,
    is_nullable,
    column_default
FROM information_schema.columns 
WHERE table_name = 'TokenBlacklists' 
ORDER BY ordinal_position;

-- 3. Show all indexes
\echo '=== INDEXES ON TokenBlacklists ==='
SELECT 
    indexname,
    indexdef
FROM pg_indexes 
WHERE tablename = 'TokenBlacklists';

-- 4. Show all constraints
\echo '=== CONSTRAINTS ON TokenBlacklists ==='
SELECT 
    conname as constraint_name,
    contype as constraint_type,
    pg_get_constraintdef(oid) as constraint_definition
FROM pg_constraint 
WHERE conrelid = 'public."TokenBlacklists"'::regclass;

-- 5. Count existing records
\echo '=== RECORD COUNT ==='
SELECT COUNT(*) as total_records FROM "TokenBlacklists";
