-- Verify current status after cleanup attempt
SELECT 
    'HEAVY DOCUMENTS STILL PRESENT' as status,
    COUNT(*) as count
FROM "Documents" d
WHERE d."FileName" LIKE '%heavy_test_document%';

-- Show all heavy documents that still exist
SELECT 
    d."Id",
    d."FileName",
    d."Status",
    d."SizeInBytes",
    d."IsProcessing",
    c."Name" as collection_name
FROM "Documents" d
JOIN "Collections" c ON d."CollectionId" = c."Id"
WHERE d."FileName" LIKE '%heavy_test_document%'
ORDER BY d."SizeInBytes" DESC
LIMIT 10;

-- Show current collection status
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
