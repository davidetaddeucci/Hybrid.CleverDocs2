-- Aggiornamento mappatura R2RUserId con ID completi da R2R
-- Escludendo admin@example.com e test@example.com (utenti default R2R)

-- Prima verifica lo stato attuale
SELECT 
    "Email", 
    "FirstName",
    "LastName",
    "R2RUserId",
    "IsActive"
FROM "Users" 
WHERE "IsActive" = true
ORDER BY "Email";

-- Aggiornamento mappature con ID completi da R2R

-- info@hybrid.it → b227d320-b01b-5acf-99f8-64d4f30ec719
UPDATE "Users" 
SET "R2RUserId" = 'b227d320-b01b-5acf-99f8-64d4f30ec719',
    "UpdatedAt" = CURRENT_TIMESTAMP
WHERE "Email" = 'info@hybrid.it' AND "IsActive" = true;

-- info@microsis.it → ef98bde5-9d89-58aa-acea-553071c67a48
UPDATE "Users" 
SET "R2RUserId" = 'ef98bde5-9d89-58aa-acea-553071c67a48',
    "UpdatedAt" = CURRENT_TIMESTAMP
WHERE "Email" = 'info@microsis.it' AND "IsActive" = true;

-- r.antoniucci@microsis.it → cf70c5e0-ca81-50cc-bb0a-30b9a80a32fb
UPDATE "Users" 
SET "R2RUserId" = 'cf70c5e0-ca81-50cc-bb0a-30b9a80a32fb',
    "UpdatedAt" = CURRENT_TIMESTAMP
WHERE "Email" = 'r.antoniucci@microsis.it' AND "IsActive" = true;

-- m.bevilacqua@microsis.it → 2db75410-5d74-5452-a450-c1a91cae94a4
UPDATE "Users" 
SET "R2RUserId" = '2db75410-5d74-5452-a450-c1a91cae94a4',
    "UpdatedAt" = CURRENT_TIMESTAMP
WHERE "Email" = 'm.bevilacqua@microsis.it' AND "IsActive" = true;

-- Verifica finale degli aggiornamenti
SELECT 
    "Email", 
    "FirstName",
    "LastName", 
    "R2RUserId",
    "IsActive",
    "UpdatedAt",
    CASE 
        WHEN "R2RUserId" IS NOT NULL AND "R2RUserId" != '' THEN '✅ SINCRONIZZATO'
        ELSE '❌ NON SINCRONIZZATO'
    END as Status
FROM "Users" 
WHERE "IsActive" = true
ORDER BY "Email";

-- Conteggio finale
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

-- Verifica specifica degli utenti mappati
SELECT 
    'MAPPATURA COMPLETATA' as Risultato,
    COUNT(*) as UtentiSincronizzati
FROM "Users" 
WHERE "IsActive" = true 
AND "R2RUserId" IS NOT NULL 
AND "R2RUserId" != '';
