-- Verify Heavy Bulk Upload Test Collection and its documents
SELECT 
    c."Id" as CollectionId,
    c."Name" as CollectionName, 
    c."R2RCollectionId",
    c."UserId",
    u."Email" as UserEmail
FROM "Collections" c
JOIN "Users" u ON c."UserId" = u."Id"
WHERE c."Name" LIKE '%Heavy Bulk Upload%' OR c."Name" LIKE '%Test%';

-- Check documents with R2RDocumentId populated
SELECT 
    d."Id",
    d."FileName",
    d."R2RDocumentId",
    d."R2RIngestionJobId",
    d."Status",
    d."UserId",
    u."Email" as UserEmail
FROM "Documents" d
JOIN "Users" u ON d."UserId" = u."Id"
WHERE d."R2RDocumentId" IS NOT NULL
ORDER BY d."CreatedAt" DESC
LIMIT 10;

-- Check if CollectionDocuments relationships exist
SELECT COUNT(*) as CollectionDocumentCount FROM "CollectionDocuments";

-- Check specific collection documents for Heavy Bulk Upload Test Collection
SELECT 
    cd."CollectionId",
    cd."DocumentId",
    c."Name" as CollectionName,
    d."FileName",
    d."R2RDocumentId"
FROM "CollectionDocuments" cd
JOIN "Collections" c ON cd."CollectionId" = c."Id"
JOIN "Documents" d ON cd."DocumentId" = d."Id"
WHERE c."Name" LIKE '%Heavy Bulk Upload%';
