-- Comprehensive Collections Schema Validation
-- Compare Entity Framework Collection model with PostgreSQL table

-- 1. Show current table structure
\echo '=== CURRENT Collections TABLE STRUCTURE ==='
\d "Collections"

-- 2. Show all columns with details
\echo '=== DETAILED COLUMN INFORMATION ==='
SELECT 
    column_name,
    data_type,
    character_maximum_length,
    is_nullable,
    column_default
FROM information_schema.columns 
WHERE table_name = 'Collections' 
ORDER BY ordinal_position;

-- 3. Show all indexes
\echo '=== INDEXES ON Collections ==='
SELECT 
    indexname,
    indexdef
FROM pg_indexes 
WHERE tablename = 'Collections';

-- 4. Show all foreign key constraints
\echo '=== FOREIGN KEY CONSTRAINTS ON Collections ==='
SELECT 
    conname as constraint_name,
    contype as constraint_type,
    pg_get_constraintdef(oid) as constraint_definition
FROM pg_constraint 
WHERE conrelid = 'public."Collections"'::regclass
AND contype = 'f';

-- 5. Count existing records
\echo '=== RECORD COUNT ==='
SELECT COUNT(*) as total_records FROM "Collections";
