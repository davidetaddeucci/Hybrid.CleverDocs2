-- Fix TenantId field in Companies table
-- The field should be UUID type and have default values

-- First, update NULL values with generated UUIDs
UPDATE "Companies" SET "TenantId" = gen_random_uuid()::text WHERE "TenantId" IS NULL;

-- Verify the update
SELECT "Id", "Name", "TenantId" FROM "Companies";
