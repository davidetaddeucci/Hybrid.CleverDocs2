-- Verifica utenti corretti nel nostro database
SELECT 
    "Id",
    "Email", 
    "Name",
    "FirstName",
    "LastName",
    "R2RUserId",
    "IsActive"
FROM "Users" 
WHERE "IsActive" = true 
AND "Email" IN (
    'admin@example.com',
    'r.antoniucci@microsis.it', 
    'info@microsis.it',
    'm.bevilacqua@microsis.it'
)
ORDER BY "Email";
