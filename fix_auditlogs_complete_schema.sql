-- Fix completo schema AuditLogs - aggiungi tutte le colonne mancanti

-- Verifica struttura attuale
\d "AuditLogs"

-- Aggiungi tutte le colonne mancanti
ALTER TABLE "AuditLogs" 
ADD COLUMN IF NOT EXISTS "IpAddress" TEXT,
ADD COLUMN IF NOT EXISTS "UserAgent" TEXT,
ADD COLUMN IF NOT EXISTS "NewValues" TEXT,
ADD COLUMN IF NOT EXISTS "OldValues" TEXT;

-- Verifica che tutte le colonne siano state aggiunte
\d "AuditLogs"

-- Test rapido
SELECT COUNT(*) as TotalAuditLogs FROM "AuditLogs";
