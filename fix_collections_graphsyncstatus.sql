-- Fix GraphClusterStatus and GraphSyncStatus data types in Collections table

-- 1. First, let's see what we have
\echo '=== CURRENT GraphClusterStatus and GraphSyncStatus COLUMNS ==='
SELECT column_name, data_type, character_maximum_length, is_nullable 
FROM information_schema.columns 
WHERE table_name = 'Collections' 
AND column_name IN ('GraphClusterStatus', 'GraphSyncStatus');

-- 2. Fix GraphClusterStatus (currently integer, should be varchar(50))
-- Since it's currently integer and should be varchar(50), we need to convert
ALTER TABLE "Collections" 
ALTER COLUMN "GraphClusterStatus" TYPE character varying(50) USING "GraphClusterStatus"::text;

-- 3. Make GraphClusterStatus nullable (as per Entity Framework model)
ALTER TABLE "Collections" 
ALTER COLUMN "GraphClusterStatus" DROP NOT NULL;

-- 4. Verify the fix
\echo '=== AFTER FIX - GraphClusterStatus and GraphSyncStatus COLUMNS ==='
SELECT column_name, data_type, character_maximum_length, is_nullable 
FROM information_schema.columns 
WHERE table_name = 'Collections' 
AND column_name IN ('GraphClusterStatus', 'GraphSyncStatus');

-- 5. Show complete table structure to verify all columns
\echo '=== COMPLETE Collections TABLE STRUCTURE ==='
SELECT 
    column_name,
    data_type,
    character_maximum_length,
    is_nullable,
    column_default
FROM information_schema.columns 
WHERE table_name = 'Collections' 
ORDER BY ordinal_position;
