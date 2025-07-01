-- Verify that Conversations and Messages tables were created successfully

-- Check if tables exist
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public' 
AND table_name IN ('Conversations', 'Messages')
ORDER BY table_name;

-- Check Conversations table structure
SELECT column_name, data_type, is_nullable, column_default 
FROM information_schema.columns 
WHERE table_schema = 'public' AND table_name = 'Conversations'
ORDER BY ordinal_position;

-- Check Messages table structure
SELECT column_name, data_type, is_nullable, column_default 
FROM information_schema.columns 
WHERE table_schema = 'public' AND table_name = 'Messages'
ORDER BY ordinal_position;

-- Check applied migrations
SELECT "MigrationId", "ProductVersion" 
FROM "__EFMigrationsHistory" 
WHERE "MigrationId" LIKE '%Chat%' OR "MigrationId" LIKE '%Conversation%' OR "MigrationId" LIKE '%Message%'
ORDER BY "MigrationId";
