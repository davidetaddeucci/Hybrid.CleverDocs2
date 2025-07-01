-- Add R2RTaskId column to Documents table for R2R progress tracking
-- Execute this script on PostgreSQL database 'cleverdocs' on 192.168.1.4:5433

-- Add the new column
ALTER TABLE "Documents" 
ADD COLUMN "R2RTaskId" TEXT NULL;

-- Add comment for documentation
COMMENT ON COLUMN "Documents"."R2RTaskId" IS 'R2R API Task ID for progress tracking - different from R2RIngestionJobId which is internal audit trail';

-- Create index for performance (TaskId will be used for lookups)
CREATE INDEX IF NOT EXISTS "IX_Documents_R2RTaskId" ON "Documents" ("R2RTaskId");

-- Verify the column was added
SELECT column_name, data_type, is_nullable 
FROM information_schema.columns 
WHERE table_name = 'Documents' 
AND column_name = 'R2RTaskId';

-- Show sample of existing documents to verify structure
SELECT "Id", "Name", "R2RDocumentId", "R2RIngestionJobId", "R2RTaskId", "Status" 
FROM "Documents" 
ORDER BY "CreatedAt" DESC 
LIMIT 5;
