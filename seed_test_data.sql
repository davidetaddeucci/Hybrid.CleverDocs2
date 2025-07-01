-- Insert test companies
INSERT INTO "Companies" ("Id", "Name", "ContactEmail", "IsActive", "MaxUsers", "MaxDocuments", "MaxStorageBytes", "MaxCollections", "CreatedAt", "UpdatedAt") 
VALUES 
('550e8400-e29b-41d4-a716-446655440000', 'Hybrid IT', 'info@hybrid.it', true, 100, 10000, 107374182400, 1000, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('550e8400-e29b-41d4-a716-446655440001', 'Microsis srl', 'info@microsis.it', true, 50, 5000, 53687091200, 500, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- Insert test users with BCrypt hashed passwords
-- Password for info@hybrid.it: Florealia2025!
-- Password for others: Maremmabona1!

INSERT INTO "Users" ("Id", "Email", "PasswordHash", "FirstName", "LastName", "Role", "CompanyId", "IsActive", "IsVerified", "CreatedAt", "UpdatedAt")
VALUES
-- System Admin (Hybrid IT) - Password: Florealia2025!
('550e8400-e29b-41d4-a716-446655440010', 'info@hybrid.it', '$2a$11$Florealia2025Hash.ExampleHashForTestingPurposesOnly', 'Admin', 'User', 1, '550e8400-e29b-41d4-a716-446655440000', true, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Company Administrator (Microsis srl) - Password: Maremmabona1!
('550e8400-e29b-41d4-a716-446655440011', 'info@microsis.it', '$2a$11$Maremmabona1Hash.ExampleHashForTestingPurposesOnly', 'Company', 'Admin', 2, '550e8400-e29b-41d4-a716-446655440001', true, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Test User 1 (Roberto Antoniucci) - Password: Maremmabona1!
('550e8400-e29b-41d4-a716-446655440012', 'r.antoniucci@microsis.it', '$2a$11$Maremmabona1Hash.ExampleHashForTestingPurposesOnly', 'Roberto', 'Antoniucci', 3, '550e8400-e29b-41d4-a716-446655440001', true, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Test User 2 (Marco Bevilacqua) - Password: Maremmabona1!
('550e8400-e29b-41d4-a716-446655440013', 'm.bevilacqua@microsis.it', '$2a$11$Maremmabona1Hash.ExampleHashForTestingPurposesOnly', 'Marco', 'Bevilacqua', 3, '550e8400-e29b-41d4-a716-446655440001', true, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
