-- Add missing CompanyId column to AuditLogs table
ALTER TABLE "AuditLogs" ADD COLUMN "CompanyId" uuid;

-- Verify the change
\d "AuditLogs";
