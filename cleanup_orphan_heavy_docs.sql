-- Find orphan heavy documents (without collection association)
SELECT 
    d."Id",
    d."FileName",
    d."Status",
    d."SizeInBytes",
    d."CollectionId",
    d."IsProcessing"
FROM "Documents" d
WHERE d."FileName" LIKE '%heavy_test_document%'
ORDER BY d."SizeInBytes" DESC;

-- Delete all heavy_test_document files regardless of collection
DELETE FROM "Documents" 
WHERE "FileName" LIKE '%heavy_test_document%';

-- Verify deletion
SELECT 
    'HEAVY DOCUMENTS AFTER CLEANUP' as status,
    COUNT(*) as count
FROM "Documents" d
WHERE d."FileName" LIKE '%heavy_test_document%';

-- Final status check
SELECT 
    c."Name" as collection_name,
    COUNT(*) as total_documents,
    COUNT(CASE WHEN d."Status" = 3 THEN 1 END) as ready_documents,
    COUNT(CASE WHEN d."Status" = 1 THEN 1 END) as processing_documents,
    COUNT(CASE WHEN d."R2RDocumentId" IS NOT NULL AND d."R2RDocumentId" != '' THEN 1 END) as with_valid_r2r_id,
    COALESCE(MAX(d."SizeInBytes"), 0) as max_file_size_bytes
FROM "Collections" c
LEFT JOIN "Documents" d ON c."Id" = d."CollectionId"
WHERE c."Name" LIKE '%Roberto%'
GROUP BY c."Id", c."Name"
ORDER BY c."Name";
