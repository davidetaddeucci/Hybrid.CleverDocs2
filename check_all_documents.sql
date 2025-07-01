-- Check all documents in Test Collection Roberto
SELECT
    d."Id",
    d."FileName",
    d."Status",
    d."R2RDocumentId",
    d."SizeInBytes",
    d."CreatedAt",
    d."R2RProcessedAt"
FROM "Documents" d
JOIN "Collections" c ON d."CollectionId" = c."Id"
WHERE c."Name" LIKE '%Roberto%'
ORDER BY d."SizeInBytes" DESC, d."CreatedAt" DESC
LIMIT 20;
