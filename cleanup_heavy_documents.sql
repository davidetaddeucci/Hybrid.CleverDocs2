-- Cleanup heavy documents that cause R2R 413 errors
-- Phase 1: Identify heavy documents
SELECT
    d."Id",
    d."FileName",
    d."Status",
    d."R2RDocumentId",
    c."Name" as collection_name,
    d."CreatedAt"
FROM "Documents" d
JOIN "Collections" c ON d."CollectionId" = c."Id"
WHERE d."FileName" LIKE '%heavy_test_document%'
ORDER BY d."CreatedAt" DESC
LIMIT 20;

-- Phase 2: Delete heavy documents (CAREFUL - this will permanently delete data)
-- Uncomment the following lines only after confirming the above query shows the right documents

-- DELETE FROM "Documents" 
-- WHERE "FileName" LIKE '%heavy_test_document%';

-- Phase 3: Reset any stuck processing documents
-- UPDATE "Documents" 
-- SET "Status" = 0, "R2RDocumentId" = NULL, "R2RProcessedAt" = NULL
-- WHERE "Status" = 1 AND "R2RDocumentId" IS NULL;

-- Phase 4: Verify cleanup
-- SELECT 
--     c."Name" as collection_name,
--     COUNT(*) as total_documents,
--     COUNT(CASE WHEN d."Status" = 3 THEN 1 END) as ready_documents,
--     COUNT(CASE WHEN d."Status" = 1 THEN 1 END) as processing_documents,
--     COUNT(CASE WHEN d."R2RDocumentId" IS NOT NULL AND d."R2RDocumentId" NOT LIKE 'pending_%' THEN 1 END) as with_valid_r2r_id
-- FROM "Collections" c
-- LEFT JOIN "Documents" d ON c."Id" = d."CollectionId"
-- GROUP BY c."Id", c."Name"
-- ORDER BY c."Name";
