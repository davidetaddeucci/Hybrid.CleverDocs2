-- Fix user role for info@hybrid.it
-- The role should be 1 (Admin) instead of 0

UPDATE "Users" 
SET "Role" = 1 
WHERE "Email" = 'info@hybrid.it';

-- Verify the update
SELECT "Email", "Role", "FirstName", "LastName" 
FROM "Users" 
WHERE "Email" = 'info@hybrid.it';
