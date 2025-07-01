-- Check Conversations table structure
\d "Conversations"

-- Check if table exists and show all columns
SELECT column_name, data_type, is_nullable, column_default
FROM information_schema.columns 
WHERE table_name = 'Conversations' 
ORDER BY ordinal_position;

-- Check if CollectionIds column exists specifically
SELECT EXISTS (
    SELECT 1 
    FROM information_schema.columns 
    WHERE table_name = 'Conversations' 
    AND column_name = 'CollectionIds'
) as collection_ids_exists;
