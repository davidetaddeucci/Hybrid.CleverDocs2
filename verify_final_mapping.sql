-- Verifica finale della mappatura R2R
SELECT 
    "Email", 
    "FirstName",
    "LastName", 
    "R2RUserId",
    "UpdatedAt",
    CASE 
        WHEN "R2RUserId" IS NOT NULL AND "R2RUserId" != '' THEN 'SINCRONIZZATO'
        ELSE 'NON SINCRONIZZATO'
    END as Status
FROM "Users" 
WHERE "IsActive" = true
ORDER BY "Email";
