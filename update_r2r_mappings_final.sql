-- Aggiornamento mappatura R2RUserId basato sui dati dalla schermata R2R
-- NOTA: Sto usando ID parziali dalla schermata - potrebbero essere incompleti

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

-- Aggiornamento mappature basate sui dati R2R dalla schermata
-- ATTENZIONE: Questi ID potrebbero essere incompleti - da verificare

-- info@hybrid.it (admin@example.com nella schermata R2R)
UPDATE "Users" 
SET "R2RUserId" = '22c47b4c-5e72-5d20-a203-5f9c3f1f8'
WHERE "Email" = 'info@hybrid.it' AND "IsActive" = true;

-- r.antoniucci@microsis.it  
UPDATE "Users" 
SET "R2RUserId" = '22c47b4c-5e72-5d20-a203-5f9c3f1f8'
WHERE "Email" = 'r.antoniucci@microsis.it' AND "IsActive" = true;

-- info@microsis.it
UPDATE "Users" 
SET "R2RUserId" = 'e7b4d5e5-3a85-48ae-8567-7c87a'
WHERE "Email" = 'info@microsis.it' AND "IsActive" = true;

-- m.bevilacqua@microsis.it (corretto dalla tua indicazione)
UPDATE "Users" 
SET "R2RUserId" = '6f3a4c5d-1c5c-4d2e-b9f7-a8e9c'
WHERE "Email" = 'm.bevilacqua@microsis.it' AND "IsActive" = true;

-- Verifica finale
SELECT 
    "Email", 
    "FirstName",
    "LastName", 
    "R2RUserId",
    "IsActive",
    CASE 
        WHEN "R2RUserId" IS NOT NULL AND "R2RUserId" != '' THEN 'SINCRONIZZATO'
        ELSE 'NON SINCRONIZZATO'
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
