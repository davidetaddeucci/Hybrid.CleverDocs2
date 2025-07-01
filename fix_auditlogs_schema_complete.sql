-- Fix completo schema AuditLogs per allinearlo alla classe C#

-- 1. Aggiungi colonne mancanti
ALTER TABLE "AuditLogs" 
ADD COLUMN IF NOT EXISTS "Details" character varying(500),
ADD COLUMN IF NOT EXISTS "IpAddress" character varying(45),
ADD COLUMN IF NOT EXISTS "UserAgent" character varying(500);

-- 2. Rimuovi colonna Timestamp duplicata (manteniamo CreatedAt)
ALTER TABLE "AuditLogs" 
DROP COLUMN IF EXISTS "Timestamp";

-- 3. Assicurati che CompanyId sia NOT NULL
ALTER TABLE "AuditLogs" 
ALTER COLUMN "CompanyId" SET NOT NULL;

-- 4. Assicurati che CreatedAt sia NOT NULL con default
ALTER TABLE "AuditLogs" 
ALTER COLUMN "CreatedAt" SET NOT NULL,
ALTER COLUMN "CreatedAt" SET DEFAULT CURRENT_TIMESTAMP;

-- Verifica schema finale
\d "AuditLogs"
