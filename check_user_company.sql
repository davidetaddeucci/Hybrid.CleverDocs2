SELECT u."Email", c."Name" as company_name, c."Id" as company_id
FROM "Users" u 
JOIN "Companies" c ON u."CompanyId" = c."Id" 
WHERE u."Email" = 'r.antoniucci@microsis.it';
