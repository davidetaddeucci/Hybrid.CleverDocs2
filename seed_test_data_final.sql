-- Insert test companies
INSERT INTO "Companies" ("Id", "Name", "ContactEmail", "IsActive", "MaxUsers", "MaxDocuments", "MaxStorageBytes", "MaxCollections", "CreatedAt", "UpdatedAt") 
VALUES 
('550e8400-e29b-41d4-a716-446655440000', 'Hybrid IT', 'info@hybrid.it', true, 100, 10000, 107374182400, 1000, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('550e8400-e29b-41d4-a716-446655440001', 'Microsis srl', 'info@microsis.it', true, 50, 5000, 53687091200, 500, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- Insert test users (using simple password hashes for testing)
-- Note: In production, these should be proper BCrypt hashes
INSERT INTO "Users" ("Id", "Email", "PasswordHash", "FirstName", "LastName", "Role", "CompanyId", "IsActive", "IsVerified", "CreatedAt", "UpdatedAt") 
VALUES 
-- System Admin (Hybrid IT) - Password: Florealia2025!
('550e8400-e29b-41d4-a716-446655440010', 'info@hybrid.it', '$2a$11$FlorealiaTempHashForTestingPurposesOnly123456789', 'Admin', 'User', 1, '550e8400-e29b-41d4-a716-446655440000', true, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Company Administrator (Microsis srl) - Password: Maremmabona1!
('550e8400-e29b-41d4-a716-446655440011', 'info@microsis.it', '$2a$11$MaremmabonaTempHashForTestingPurposesOnly123456789', 'Company', 'Admin', 2, '550e8400-e29b-41d4-a716-446655440001', true, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Test User 1 (Roberto Antoniucci) - Password: Maremmabona1!
('550e8400-e29b-41d4-a716-446655440012', 'r.antoniucci@microsis.it', '$2a$11$MaremmabonaTempHashForTestingPurposesOnly123456789', 'Roberto', 'Antoniucci', 3, '550e8400-e29b-41d4-a716-446655440001', true, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Test User 2 (Marco Bevilacqua) - Password: Maremmabona1!
('550e8400-e29b-41d4-a716-446655440013', 'm.bevilacqua@microsis.it', '$2a$11$MaremmabonaTempHashForTestingPurposesOnly123456789', 'Marco', 'Bevilacqua', 3, '550e8400-e29b-41d4-a716-446655440001', true, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
