-- Verifica stato sincronizzazione utenti dopo il test R2R

-- 1. Stato attuale utenti nel database
SELECT 
    "Id",
    "Email", 
    "Name",
    "R2RUserId",
    "IsActive",
    "UpdatedAt"
FROM "Users" 
WHERE "IsActive" = true
ORDER BY "Email";

-- 2. Conteggio utenti sincronizzati vs non sincronizzati
SELECT 
    CASE 
        WHEN "R2RUserId" IS NOT NULL AND "R2RUserId" != '' THEN 'Sincronizzati'
        ELSE 'Non Sincronizzati'
    END as Status,
    COUNT(*) as Count
FROM "Users" 
WHERE "IsActive" = true
GROUP BY 
    CASE 
        WHEN "R2RUserId" IS NOT NULL AND "R2RUserId" != '' THEN 'Sincronizzati'
        ELSE 'Non Sincronizzati'
    END;
