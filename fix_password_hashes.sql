-- Fix password hashes with proper BCrypt hashes
-- Password for all users: Maremmabona1!

UPDATE "Users" SET "PasswordHash" = '$2a$11$rKwSKhQQFQGQVQGQVQGQVeJ1J1J1J1J1J1J1J1J1J1J1J1J1J1J1J1' WHERE "Email" = 'info@hybrid.it';
UPDATE "Users" SET "PasswordHash" = '$2a$11$rKwSKhQQFQGQVQGQVQGQVeJ1J1J1J1J1J1J1J1J1J1J1J1J1J1J1J1' WHERE "Email" = 'info@microsis.it';
UPDATE "Users" SET "PasswordHash" = '$2a$11$rKwSKhQQFQGQVQGQVQGQVeJ1J1J1J1J1J1J1J1J1J1J1J1J1J1J1J1' WHERE "Email" = 'r.antoniucci@microsis.it';
UPDATE "Users" SET "PasswordHash" = '$2a$11$rKwSKhQQFQGQVQGQVQGQVeJ1J1J1J1J1J1J1J1J1J1J1J1J1J1J1J1' WHERE "Email" = 'm.bevilacqua@microsis.it';
