-- CRITICAL FIX: Collections table schema alignment with Entity Framework model

-- 1. Add missing columns
ALTER TABLE "Collections" 
ADD COLUMN IF NOT EXISTS "TagsJson" text,
ADD COLUMN IF NOT EXISTS "LastSyncedAt" timestamp with time zone,
ADD COLUMN IF NOT EXISTS "GraphClusterStatus" character varying(50),
ADD COLUMN IF NOT EXISTS "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
ADD COLUMN IF NOT EXISTS "UpdatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
ADD COLUMN IF NOT EXISTS "CreatedBy" character varying(255),
ADD COLUMN IF NOT EXISTS "UpdatedBy" character varying(255);

-- 2. Fix Icon column length (100 -> 50)
ALTER TABLE "Collections" 
ALTER COLUMN "Icon" TYPE character varying(50);

-- 3. Fix GraphSyncStatus data type (integer -> varchar(50))
-- First drop the column and recreate it with correct type
ALTER TABLE "Collections" 
DROP COLUMN IF EXISTS "GraphSyncStatus";

ALTER TABLE "Collections" 
ADD COLUMN "GraphSyncStatus" character varying(50);

-- 4. Set default values for existing records (if any)
UPDATE "Collections" 
SET 
    "CreatedAt" = COALESCE("CreatedAt", CURRENT_TIMESTAMP),
    "UpdatedAt" = COALESCE("UpdatedAt", CURRENT_TIMESTAMP),
    "Icon" = COALESCE("Icon", 'folder'),
    "Color" = COALESCE("Color", '#3B82F6')
WHERE "CreatedAt" IS NULL OR "UpdatedAt" IS NULL;

-- 5. Verify final schema
\echo '=== FINAL Collections SCHEMA VERIFICATION ==='
\d "Collections"

-- 6. Show all columns with details
\echo '=== FINAL COLUMN VERIFICATION ==='
SELECT 
    column_name,
    data_type,
    character_maximum_length,
    is_nullable,
    column_default
FROM information_schema.columns 
WHERE table_name = 'Collections' 
ORDER BY ordinal_position;
