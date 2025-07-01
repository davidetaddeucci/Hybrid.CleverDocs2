-- Lista tutti gli utenti attivi nel database
SELECT 
    "Id",
    "Email", 
    "FirstName",
    "LastName",
    "R2RUserId"
FROM "Users" 
WHERE "IsActive" = true
ORDER BY "Email";
