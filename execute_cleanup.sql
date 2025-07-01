-- Execute cleanup of large documents that cause R2R 413 errors
-- Delete documents larger than 10MB
DELETE FROM "Documents" 
WHERE "SizeInBytes" > 10000000;

-- Reset any stuck processing documents
UPDATE "Documents" 
SET "Status" = 0, "R2RDocumentId" = NULL, "R2RProcessedAt" = NULL
WHERE "Status" = 1 AND ("R2RDocumentId" IS NULL OR "R2RDocumentId" = '');

-- Verify cleanup results
SELECT 
    c."Name" as collection_name,
    COUNT(*) as total_documents,
    COUNT(CASE WHEN d."Status" = 3 THEN 1 END) as ready_documents,
    COUNT(CASE WHEN d."Status" = 1 THEN 1 END) as processing_documents,
    COUNT(CASE WHEN d."R2RDocumentId" IS NOT NULL AND d."R2RDocumentId" != '' THEN 1 END) as with_valid_r2r_id,
    COALESCE(MAX(d."SizeInBytes"), 0) as max_file_size_bytes
FROM "Collections" c
LEFT JOIN "Documents" d ON c."Id" = d."CollectionId"
GROUP BY c."Id", c."Name"
ORDER BY c."Name";
