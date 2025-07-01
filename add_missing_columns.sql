-- Add missing CollectionIds column to Conversations table
-- This column stores JSON array of collection IDs associated with the conversation

BEGIN;

-- Add CollectionIds column as JSONB (PostgreSQL native JSON type)
ALTER TABLE "Conversations" 
ADD COLUMN "CollectionIds" JSONB NOT NULL DEFAULT '[]'::jsonb;

-- Add comment for documentation
COMMENT ON COLUMN "Conversations"."CollectionIds" IS 'JSON array of collection IDs associated with this conversation';

-- Add index for better performance on JSON queries
CREATE INDEX IF NOT EXISTS "IX_Conversations_CollectionIds" ON "Conversations" USING GIN ("CollectionIds");

-- Verify the column was added
SELECT column_name, data_type, is_nullable, column_default
FROM information_schema.columns 
WHERE table_name = 'Conversations' 
AND column_name = 'CollectionIds';

-- Show updated table structure
\d "Conversations"

COMMIT;
