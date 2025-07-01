-- Update password hashes with real BCrypt hashes
UPDATE "Users" SET "PasswordHash" = '$2a$12$UvnXDIKQBGJpk9Jplr4R6.QQ2YNdwQnNy8zczboloTSG2J8QpLxpu' WHERE "Email" = 'info@hybrid.it';
UPDATE "Users" SET "PasswordHash" = '$2a$12$XLdS.6tTPPrT1Q1SlVKVAOr/..sevSVIqFIbcunDCTRZbEl/Rtaja' WHERE "Email" = 'info@microsis.it';
UPDATE "Users" SET "PasswordHash" = '$2a$12$XLdS.6tTPPrT1Q1SlVKVAOr/..sevSVIqFIbcunDCTRZbEl/Rtaja' WHERE "Email" = 'r.antoniucci@microsis.it';
UPDATE "Users" SET "PasswordHash" = '$2a$12$XLdS.6tTPPrT1Q1SlVKVAOr/..sevSVIqFIbcunDCTRZbEl/Rtaja' WHERE "Email" = 'm.bevilacqua@microsis.it';
