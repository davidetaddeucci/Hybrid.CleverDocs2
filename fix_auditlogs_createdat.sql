-- Add missing CreatedAt column to AuditLogs table
ALTER TABLE "AuditLogs" ADD COLUMN "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP;

-- Verify the change
\d "AuditLogs";
