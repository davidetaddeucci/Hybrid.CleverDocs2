-- Check Messages table schema
SELECT column_name, data_type, is_nullable, column_default 
FROM information_schema.columns 
WHERE table_schema = 'public' AND table_name = 'Messages'
ORDER BY ordinal_position;

-- Check Conversations table schema  
SELECT column_name, data_type, is_nullable, column_default 
FROM information_schema.columns 
WHERE table_schema = 'public' AND table_name = 'Conversations'
ORDER BY ordinal_position;

-- Check applied migrations
SELECT "MigrationId", "ProductVersion" 
FROM "__EFMigrationsHistory" 
ORDER BY "MigrationId" DESC;
