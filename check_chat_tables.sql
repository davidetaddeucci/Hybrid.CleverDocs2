-- Check if Conversations and Messages tables exist
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public' 
AND table_name IN ('Conversations', 'Messages');

-- Check applied migrations
SELECT "MigrationId", "ProductVersion" 
FROM "__EFMigrationsHistory" 
WHERE "MigrationId" LIKE '%Chat%' OR "MigrationId" LIKE '%Conversation%' OR "MigrationId" LIKE '%Message%'
ORDER BY "MigrationId";
