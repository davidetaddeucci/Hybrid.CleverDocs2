-- Fix TokenBlacklists table schema - Aggiungi colonna UserId mancante

-- Verifica schema attuale
\d "TokenBlacklists"

-- Aggiungi colonna UserId se non esiste
ALTER TABLE "TokenBlacklists" 
ADD COLUMN IF NOT EXISTS "UserId" uuid;

-- Crea indice per UserId come nelle migrations
CREATE INDEX IF NOT EXISTS "IX_TokenBlacklists_UserId" ON "TokenBlacklists" ("UserId");

-- Verifica schema finale
\d "TokenBlacklists"

-- Test rapido
SELECT COUNT(*) as TotalTokenBlacklists FROM "TokenBlacklists";
