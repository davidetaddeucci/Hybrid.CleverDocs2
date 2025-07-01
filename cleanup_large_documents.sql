-- PHASE 1: Backup large documents info before deletion
CREATE TEMP TABLE large_docs_backup AS
SELECT 
    d."Id",
    d."FileName",
    d."SizeInBytes",
    d."Status",
    c."Name" as collection_name
FROM "Documents" d
JOIN "Collections" c ON d."CollectionId" = c."Id"
WHERE d."SizeInBytes" > 10000000; -- Files larger than 10MB

-- Show what will be deleted
SELECT 
    collection_name,
    "FileName",
    ROUND("SizeInBytes"::numeric / 1024 / 1024, 2) as size_mb,
    "Status"
FROM large_docs_backup
ORDER BY "SizeInBytes" DESC;

-- PHASE 2: Delete large documents (UNCOMMENT TO EXECUTE)
-- DELETE FROM "Documents" 
-- WHERE "SizeInBytes" > 10000000;

-- PHASE 3: Reset any stuck processing documents
-- UPDATE "Documents" 
-- SET "Status" = 0, "R2RDocumentId" = NULL, "R2RProcessedAt" = NULL
-- WHERE "Status" = 1 AND ("R2RDocumentId" IS NULL OR "R2RDocumentId" = '');

-- PHASE 4: Verify cleanup results
-- SELECT 
--     c."Name" as collection_name,
--     COUNT(*) as total_documents,
--     COUNT(CASE WHEN d."Status" = 3 THEN 1 END) as ready_documents,
--     COUNT(CASE WHEN d."Status" = 1 THEN 1 END) as processing_documents,
--     COUNT(CASE WHEN d."R2RDocumentId" IS NOT NULL AND d."R2RDocumentId" != '' THEN 1 END) as with_valid_r2r_id,
--     MAX(d."SizeInBytes") as max_file_size_bytes
-- FROM "Collections" c
-- LEFT JOIN "Documents" d ON c."Id" = d."CollectionId"
-- GROUP BY c."Id", c."Name"
-- ORDER BY c."Name";
