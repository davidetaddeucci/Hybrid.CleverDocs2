-- Query to find Documents with R2RDocumentId
SELECT
    d."Id" as DocumentId,
    d."Name",
    d."OriginalFileName",
    d."R2RDocumentId",
    d."R2RIngestionJobId",
    d."Status",
    d."CompanyId",
    d."UserId"
FROM "Documents" d
WHERE d."R2RDocumentId" IS NOT NULL
ORDER BY d."CreatedAt" DESC
LIMIT 20;
