-- Query to find Collections with their R2R-processed documents
SELECT 
    c."Id" as CollectionId,
    c."Name" as CollectionName,
    c."R2RCollectionId",
    d."Id" as DocumentId,
    d."Name" as DocumentName,
    d."R2RDocumentId",
    d."Status" as DocumentStatus,
    u."Email" as UserEmail
FROM "Collections" c 
INNER JOIN "CollectionDocuments" cd ON c."Id" = cd."CollectionId"
INNER JOIN "Documents" d ON cd."DocumentId" = d."Id"
INNER JOIN "Users" u ON d."UserId" = u."Id"
WHERE d."R2RDocumentId" IS NOT NULL
ORDER BY c."Name", d."Name"
LIMIT 20;
