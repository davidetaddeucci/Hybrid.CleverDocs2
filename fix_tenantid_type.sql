-- Fix TenantId field type from character varying to uuid
-- First, convert the string values to proper UUIDs

-- Update the column type from character varying to uuid
ALTER TABLE "Companies" ALTER COLUMN "TenantId" TYPE uuid USING "TenantId"::uuid;

-- Verify the change
SELECT "Id", "Name", "TenantId" FROM "Companies";
