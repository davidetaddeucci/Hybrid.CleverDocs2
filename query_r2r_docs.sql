-- Documenti con R2RDocumentId popolato
SELECT "Id", "Name", "OriginalFileName", "Status", "R2RDocumentId", "R2RIngestionJobId", "CreatedAt", "UpdatedAt" 
FROM "Documents" 
WHERE "R2RDocumentId" IS NOT NULL AND "R2RDocumentId" != ''
ORDER BY "CreatedAt" DESC 
LIMIT 5;

-- Conteggio documenti per status
SELECT "Status", COUNT(*) as "Count"
FROM "Documents"
GROUP BY "Status";

-- Documenti recenti senza R2RDocumentId
SELECT "Id", "Name", "Status", "R2RDocumentId", "CreatedAt"
FROM "Documents" 
WHERE ("R2RDocumentId" IS NULL OR "R2RDocumentId" = '') 
AND "CreatedAt" > NOW() - INTERVAL '1 hour'
ORDER BY "CreatedAt" DESC;
