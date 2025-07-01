-- Check if Conversations and Messages tables exist
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public' 
AND table_name IN ('Conversations', 'Messages');

-- Check Documents table columns
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_schema = 'public' 
AND table_name = 'Documents'
ORDER BY ordinal_position;
