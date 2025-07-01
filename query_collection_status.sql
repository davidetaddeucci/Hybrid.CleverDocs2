-- Check CollectionDocuments table status
SELECT COUNT(*) as TotalCollectionDocuments FROM "CollectionDocuments";

-- Check Collections with user info
SELECT 
    c."Id" as CollectionId,
    c."Name" as CollectionName,
    c."R2RCollectionId",
    u."Email" as UserEmail,
    c."CreatedAt"
FROM "Collections" c 
INNER JOIN "Users" u ON c."UserId" = u."Id"
ORDER BY c."CreatedAt" DESC
LIMIT 10;
