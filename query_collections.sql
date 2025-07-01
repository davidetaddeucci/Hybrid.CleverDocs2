-- Query to find Collections with R2R-processed documents
SELECT 
    c."Id" as CollectionId,
    c."Name" as CollectionName,
    c."R2RCollectionId",
    COUNT(d."Id") as TotalDocuments,
    COUNT(CASE WHEN d."R2RDocumentId" IS NOT NULL THEN 1 END) as ProcessedDocuments,
    STRING_AGG(CASE WHEN d."R2RDocumentId" IS NOT NULL THEN d."R2RDocumentId" END, ', ') as R2RDocumentIds
FROM "Collections" c 
LEFT JOIN "CollectionDocuments" cd ON c."Id" = cd."CollectionId"
LEFT JOIN "Documents" d ON cd."DocumentId" = d."Id"
GROUP BY c."Id", c."Name", c."R2RCollectionId"
ORDER BY ProcessedDocuments DESC, TotalDocuments DESC
LIMIT 10;
