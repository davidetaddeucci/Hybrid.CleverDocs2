-- CleverDocs2 Database Initialization Script
-- This script sets up the initial database structure for Hybrid.CleverDocs2

-- Create additional databases if needed
CREATE DATABASE cleverdocs_test;
CREATE DATABASE cleverdocs_prod;

-- Create additional users
CREATE USER cleverdocs_user WITH PASSWORD 'cleverdocs_strong_password';
CREATE USER cleverdocs_readonly WITH PASSWORD 'readonly_password';

-- Grant permissions
GRANT ALL PRIVILEGES ON DATABASE cleverdocs_dev TO cleverdocs_dev;
GRANT ALL PRIVILEGES ON DATABASE cleverdocs_test TO cleverdocs_dev;
GRANT ALL PRIVILEGES ON DATABASE cleverdocs_prod TO cleverdocs_user;
GRANT CONNECT ON DATABASE cleverdocs_dev TO cleverdocs_readonly;
GRANT CONNECT ON DATABASE cleverdocs_test TO cleverdocs_readonly;
GRANT CONNECT ON DATABASE cleverdocs_prod TO cleverdocs_readonly;

-- Connect to the main development database
\c cleverdocs_dev;

-- Enable required extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";
CREATE EXTENSION IF NOT EXISTS "vector";

-- Create schemas for organization
CREATE SCHEMA IF NOT EXISTS auth;
CREATE SCHEMA IF NOT EXISTS documents;
CREATE SCHEMA IF NOT EXISTS collections;
CREATE SCHEMA IF NOT EXISTS analytics;
CREATE SCHEMA IF NOT EXISTS system;

-- Grant schema permissions
GRANT USAGE ON SCHEMA auth TO cleverdocs_dev, cleverdocs_readonly;
GRANT USAGE ON SCHEMA documents TO cleverdocs_dev, cleverdocs_readonly;
GRANT USAGE ON SCHEMA collections TO cleverdocs_dev, cleverdocs_readonly;
GRANT USAGE ON SCHEMA analytics TO cleverdocs_dev, cleverdocs_readonly;
GRANT USAGE ON SCHEMA system TO cleverdocs_dev, cleverdocs_readonly;

-- Create basic tables for CleverDocs2 (these will be managed by Entity Framework migrations)
-- This is just to ensure the database is properly set up

-- System configuration table
CREATE TABLE IF NOT EXISTS system.configuration (
    id SERIAL PRIMARY KEY,
    key VARCHAR(255) UNIQUE NOT NULL,
    value TEXT,
    description TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Insert default configuration
INSERT INTO system.configuration (key, value, description) VALUES
('system.version', '1.0.0', 'CleverDocs2 system version'),
('system.initialized', 'true', 'System initialization flag'),
('r2r.api_url', 'http://r2r-api:7272', 'R2R API base URL'),
('features.collections_enabled', 'true', 'Collections feature flag'),
('features.auth_enabled', 'true', 'Authentication feature flag')
ON CONFLICT (key) DO NOTHING;

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS idx_configuration_key ON system.configuration(key);

-- Grant table permissions
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA system TO cleverdocs_dev;
GRANT SELECT ON ALL TABLES IN SCHEMA system TO cleverdocs_readonly;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA system TO cleverdocs_dev;

-- Create a function to update the updated_at timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Create trigger for configuration table
DROP TRIGGER IF EXISTS update_configuration_updated_at ON system.configuration;
CREATE TRIGGER update_configuration_updated_at
    BEFORE UPDATE ON system.configuration
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Log successful initialization
INSERT INTO system.configuration (key, value, description) VALUES
('system.last_init', NOW()::TEXT, 'Last database initialization timestamp')
ON CONFLICT (key) DO UPDATE SET value = NOW()::TEXT, updated_at = NOW();