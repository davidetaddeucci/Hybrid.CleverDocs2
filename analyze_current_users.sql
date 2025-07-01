-- Analisi stato attuale utenti prima della sincronizzazione R2R

-- 1. Conteggio utenti attivi
SELECT 'Active Users' as metric, COUNT(*) as count 
FROM "Users" 
WHERE "IsActive" = true;

-- 2. Utenti con R2RUserId esistente (dovrebbero essere tutti obsoleti dopo il reset)
SELECT 'Users with R2RUserId' as metric, COUNT(*) as count 
FROM "Users" 
WHERE "R2RUserId" IS NOT NULL AND "R2RUserId" != '';

-- 3. Dettagli utenti attivi
SELECT 
    "Id",
    "Email", 
    "FirstName",
    "LastName",
    "Name",
    "CompanyId",
    "R2RUserId",
    "IsActive",
    "CreatedAt"
FROM "Users" 
WHERE "IsActive" = true
ORDER BY "CreatedAt";

-- 4. Conteggio per company
SELECT 
    c."Name" as CompanyName,
    c."TenantId",
    c."R2RTenantId",
    COUNT(u."Id") as UserCount
FROM "Companies" c
LEFT JOIN "Users" u ON c."Id" = u."CompanyId" AND u."IsActive" = true
GROUP BY c."Id", c."Name", c."TenantId", c."R2RTenantId"
ORDER BY UserCount DESC;
