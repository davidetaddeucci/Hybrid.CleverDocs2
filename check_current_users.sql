-- Verifica utenti attuali nel nostro database
SELECT 
    "Id",
    "Email", 
    "Name",
    "FirstName",
    "LastName",
    "R2RUserId",
    "IsActive",
    "CreatedAt"
FROM "Users" 
WHERE "IsActive" = true
ORDER BY "Email";
