-- Fix TokenBlacklists table schema per allinearlo alla classe C#

-- Verifica schema attuale
\d "TokenBlacklists"

-- Aggiungi colonna Reason se non esiste
ALTER TABLE "TokenBlacklists" 
ADD COLUMN IF NOT EXISTS "Reason" character varying(50);

-- Verifica schema finale
\d "TokenBlacklists"

-- Test rapido
SELECT COUNT(*) as TotalTokenBlacklists FROM "TokenBlacklists";
