-- Fix AuditLogs table - aggiungi colonna Details mancante

-- Verifica struttura attuale
\d "AuditLogs"

-- Aggiungi colonna Details se non esiste
ALTER TABLE "AuditLogs" 
ADD COLUMN IF NOT EXISTS "Details" TEXT;

-- Verifica che la colonna sia stata aggiunta
\d "AuditLogs"

-- Test rapido
SELECT COUNT(*) as TotalAuditLogs FROM "AuditLogs";
