-- SQLite initialization script for CleverDocs2
-- Creates tables for authentication and authorization

-- Companies table
CREATE TABLE IF NOT EXISTS companies (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    email TEXT NOT NULL UNIQUE,
    logo_url TEXT,
    phone TEXT,
    address TEXT,
    website TEXT,
    industry TEXT,
    size TEXT,
    is_active INTEGER NOT NULL DEFAULT 1,
    subscription_plan TEXT NOT NULL DEFAULT 'Free',
    subscription_expires TEXT,
    max_users INTEGER NOT NULL DEFAULT 10,
    max_documents INTEGER NOT NULL DEFAULT 1000,
    max_storage_gb INTEGER NOT NULL DEFAULT 5,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Users table
CREATE TABLE IF NOT EXISTS users (
    id TEXT PRIMARY KEY,
    email TEXT NOT NULL UNIQUE,
    password_hash TEXT NOT NULL,
    first_name TEXT NOT NULL,
    last_name TEXT NOT NULL,
    role TEXT NOT NULL DEFAULT 'User',
    company_id TEXT,
    avatar_url TEXT,
    phone TEXT,
    is_active INTEGER NOT NULL DEFAULT 1,
    is_email_verified INTEGER NOT NULL DEFAULT 0,
    email_verification_token TEXT,
    password_reset_token TEXT,
    password_reset_expires TEXT,
    last_login TEXT,
    login_count INTEGER NOT NULL DEFAULT 0,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (company_id) REFERENCES companies(id) ON DELETE SET NULL
);

-- Refresh tokens table
CREATE TABLE IF NOT EXISTS refresh_tokens (
    id TEXT PRIMARY KEY,
    token TEXT NOT NULL UNIQUE,
    user_id TEXT NOT NULL,
    expires TEXT NOT NULL,
    created TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by_ip TEXT,
    revoked TEXT,
    revoked_by_ip TEXT,
    replaced_by_token TEXT,
    reason_revoked TEXT,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);

-- User sessions table
CREATE TABLE IF NOT EXISTS user_sessions (
    id TEXT PRIMARY KEY,
    user_id TEXT NOT NULL,
    session_token TEXT NOT NULL UNIQUE,
    ip_address TEXT,
    user_agent TEXT,
    device_info TEXT,
    location TEXT,
    is_active INTEGER NOT NULL DEFAULT 1,
    last_activity TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    expires_at TEXT NOT NULL,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);

-- Indexes for performance
CREATE INDEX IF NOT EXISTS idx_users_email ON users(email);
CREATE INDEX IF NOT EXISTS idx_users_company_id ON users(company_id);
CREATE INDEX IF NOT EXISTS idx_users_role ON users(role);
CREATE INDEX IF NOT EXISTS idx_users_is_active ON users(is_active);
CREATE INDEX IF NOT EXISTS idx_users_created_at ON users(created_at);

CREATE INDEX IF NOT EXISTS idx_companies_email ON companies(email);
CREATE INDEX IF NOT EXISTS idx_companies_name ON companies(name);
CREATE INDEX IF NOT EXISTS idx_companies_is_active ON companies(is_active);
CREATE INDEX IF NOT EXISTS idx_companies_subscription_plan ON companies(subscription_plan);
CREATE INDEX IF NOT EXISTS idx_companies_created_at ON companies(created_at);

CREATE INDEX IF NOT EXISTS idx_refresh_tokens_token ON refresh_tokens(token);
CREATE INDEX IF NOT EXISTS idx_refresh_tokens_user_id ON refresh_tokens(user_id);
CREATE INDEX IF NOT EXISTS idx_refresh_tokens_expires ON refresh_tokens(expires);
CREATE INDEX IF NOT EXISTS idx_refresh_tokens_created ON refresh_tokens(created);

CREATE INDEX IF NOT EXISTS idx_user_sessions_session_token ON user_sessions(session_token);
CREATE INDEX IF NOT EXISTS idx_user_sessions_user_id ON user_sessions(user_id);
CREATE INDEX IF NOT EXISTS idx_user_sessions_is_active ON user_sessions(is_active);
CREATE INDEX IF NOT EXISTS idx_user_sessions_last_activity ON user_sessions(last_activity);
CREATE INDEX IF NOT EXISTS idx_user_sessions_expires_at ON user_sessions(expires_at);

-- Seed data
-- Insert default admin company
INSERT OR IGNORE INTO companies (
    id, name, email, is_active, subscription_plan, max_users, max_documents, max_storage_gb
) VALUES (
    'admin-company-001', 
    'CleverDocs Administration', 
    'admin@cleverdocs.ai', 
    1, 
    'Enterprise', 
    1000, 
    100000, 
    1000
);

-- Insert demo company
INSERT OR IGNORE INTO companies (
    id, name, email, phone, address, website, industry, size, is_active, 
    subscription_plan, max_users, max_documents, max_storage_gb
) VALUES (
    'demo-company-001', 
    'Acme Corporation', 
    'admin@acme.com', 
    '+39 02 1234567', 
    'Via Roma 123, Milano, Italy', 
    'https://acme.com', 
    'Technology', 
    'Medium', 
    1, 
    'Professional', 
    50, 
    10000, 
    100
);

-- Insert Hybrid company
INSERT OR IGNORE INTO companies (
    id, name, email, phone, address, website, industry, size, is_active, 
    subscription_plan, max_users, max_documents, max_storage_gb
) VALUES (
    'hybrid-company-001', 
    'Hybrid Solutions', 
    'info@hybrid.it', 
    '+39 06 12345678', 
    'Via del Corso 123, Roma, Italy', 
    'https://hybrid.it', 
    'Technology Consulting', 
    'Medium', 
    1, 
    'Enterprise', 
    100, 
    50000, 
    500
);

-- Insert admin user (password: admin123)
INSERT OR IGNORE INTO users (
    id, email, password_hash, first_name, last_name, role, company_id, 
    is_active, is_email_verified
) VALUES (
    'admin-user-001', 
    'admin@cleverdocs.ai', 
    '$2a$11$rOzJqQZJqQZJqQZJqQZJqOzJqQZJqQZJqQZJqQZJqQZJqQZJqQZJq', -- admin123
    'System', 
    'Administrator', 
    'Admin', 
    'admin-company-001', 
    1, 
    1
);

-- Insert company admin user (password: company123)
INSERT OR IGNORE INTO users (
    id, email, password_hash, first_name, last_name, role, company_id, 
    is_active, is_email_verified
) VALUES (
    'company-user-001', 
    'company@example.com', 
    '$2a$11$rOzJqQZJqQZJqQZJqQZJqOzJqQZJqQZJqQZJqQZJqQZJqQZJqQZJq', -- company123
    'Company', 
    'Manager', 
    'Company', 
    'demo-company-001', 
    1, 
    1
);

-- Insert regular user (password: user123)
INSERT OR IGNORE INTO users (
    id, email, password_hash, first_name, last_name, role, company_id, 
    is_active, is_email_verified
) VALUES (
    'regular-user-001', 
    'user@example.com', 
    '$2a$11$rOzJqQZJqQZJqQZJqQZJqOzJqQZJqQZJqQZJqQZJqQZJqQZJqQZJq', -- user123
    'Mario', 
    'Rossi', 
    'User', 
    'demo-company-001', 
    1, 
    1
);

-- Insert Hybrid admin user (password: Florealia2025!)
INSERT OR IGNORE INTO users (
    id, email, password_hash, first_name, last_name, role, company_id, 
    is_active, is_email_verified
) VALUES (
    'hybrid-admin-001', 
    'info@hybrid.it', 
    '$2a$11$rOzJqQZJqQZJqQZJqQZJqOzJqQZJqQZJqQZJqQZJqQZJqQZJqQZJq', -- Florealia2025!
    'Hybrid', 
    'Administrator', 
    'Admin', 
    'hybrid-company-001', 
    1, 
    1
);