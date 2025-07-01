-- Reset di tutti i R2RUserId obsoleti dopo il reset di R2R

-- 1. Backup delle mappature esistenti (per sicurezza)
CREATE TEMP TABLE user_r2r_backup AS
SELECT "Id", "Email", "R2RUserId", "UpdatedAt"
FROM "Users" 
WHERE "R2RUserId" IS NOT NULL AND "R2RUserId" != '';

-- 2. Mostra il backup
SELECT 'Backup created for' as info, COUNT(*) as users_backed_up 
FROM user_r2r_backup;

-- 3. Reset di tutti i R2RUserId
UPDATE "Users" 
SET "R2RUserId" = NULL, 
    "UpdatedAt" = CURRENT_TIMESTAMP
WHERE "R2RUserId" IS NOT NULL AND "R2RUserId" != '';

-- 4. Verifica del reset
SELECT 'Users with R2RUserId after reset' as metric, COUNT(*) as count 
FROM "Users" 
WHERE "R2RUserId" IS NOT NULL AND "R2RUserId" != '';

-- 5. Conferma utenti pronti per sincronizzazione
SELECT 'Users ready for R2R sync' as metric, COUNT(*) as count 
FROM "Users" 
WHERE "IsActive" = true AND ("R2RUserId" IS NULL OR "R2RUserId" = '');
