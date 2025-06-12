-- Initial Auth Migration for CleverDocs2
-- Creates tables for authentication and authorization

-- Companies table
CREATE TABLE companies (
    id VARCHAR(255) PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    email VARCHAR(255) NOT NULL UNIQUE,
    logo_url VARCHAR(500),
    phone VARCHAR(20),
    address VARCHAR(500),
    website VARCHAR(255),
    industry VARCHAR(100),
    size VARCHAR(50),
    is_active BOOLEAN NOT NULL DEFAULT true,
    subscription_plan VARCHAR(50) NOT NULL DEFAULT 'Free',
    subscription_expires TIMESTAMP,
    max_users INTEGER NOT NULL DEFAULT 10,
    max_documents INTEGER NOT NULL DEFAULT 1000,
    max_storage_gb INTEGER NOT NULL DEFAULT 5,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Users table
CREATE TABLE users (
    id VARCHAR(255) PRIMARY KEY,
    email VARCHAR(255) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    role VARCHAR(50) NOT NULL DEFAULT 'User',
    company_id VARCHAR(255),
    avatar_url VARCHAR(500),
    phone VARCHAR(20),
    is_active BOOLEAN NOT NULL DEFAULT true,
    is_email_verified BOOLEAN NOT NULL DEFAULT false,
    email_verification_token VARCHAR(255),
    password_reset_token VARCHAR(255),
    password_reset_expires TIMESTAMP,
    last_login TIMESTAMP,
    login_count INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (company_id) REFERENCES companies(id) ON DELETE SET NULL
);

-- Refresh tokens table
CREATE TABLE refresh_tokens (
    id VARCHAR(255) PRIMARY KEY,
    token VARCHAR(255) NOT NULL UNIQUE,
    user_id VARCHAR(255) NOT NULL,
    expires TIMESTAMP NOT NULL,
    created TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by_ip VARCHAR(45),
    revoked TIMESTAMP,
    revoked_by_ip VARCHAR(45),
    replaced_by_token VARCHAR(255),
    reason_revoked VARCHAR(255),
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);

-- User sessions table
CREATE TABLE user_sessions (
    id VARCHAR(255) PRIMARY KEY,
    user_id VARCHAR(255) NOT NULL,
    session_token VARCHAR(255) NOT NULL UNIQUE,
    ip_address VARCHAR(45),
    user_agent VARCHAR(500),
    device_info VARCHAR(255),
    location VARCHAR(255),
    is_active BOOLEAN NOT NULL DEFAULT true,
    last_activity TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    expires_at TIMESTAMP NOT NULL,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);

-- Indexes for performance
CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_users_company_id ON users(company_id);
CREATE INDEX idx_users_role ON users(role);
CREATE INDEX idx_users_is_active ON users(is_active);
CREATE INDEX idx_users_created_at ON users(created_at);

CREATE INDEX idx_companies_email ON companies(email);
CREATE INDEX idx_companies_name ON companies(name);
CREATE INDEX idx_companies_is_active ON companies(is_active);
CREATE INDEX idx_companies_subscription_plan ON companies(subscription_plan);
CREATE INDEX idx_companies_created_at ON companies(created_at);

CREATE INDEX idx_refresh_tokens_token ON refresh_tokens(token);
CREATE INDEX idx_refresh_tokens_user_id ON refresh_tokens(user_id);
CREATE INDEX idx_refresh_tokens_expires ON refresh_tokens(expires);
CREATE INDEX idx_refresh_tokens_created ON refresh_tokens(created);

CREATE INDEX idx_user_sessions_session_token ON user_sessions(session_token);
CREATE INDEX idx_user_sessions_user_id ON user_sessions(user_id);
CREATE INDEX idx_user_sessions_is_active ON user_sessions(is_active);
CREATE INDEX idx_user_sessions_last_activity ON user_sessions(last_activity);
CREATE INDEX idx_user_sessions_expires_at ON user_sessions(expires_at);

-- Seed data
-- Insert default admin company
INSERT INTO companies (
    id, name, email, is_active, subscription_plan, max_users, max_documents, max_storage_gb
) VALUES (
    'admin-company-001', 
    'CleverDocs Administration', 
    'admin@cleverdocs.ai', 
    true, 
    'Enterprise', 
    1000, 
    100000, 
    1000
);

-- Insert demo company
INSERT INTO companies (
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
    true, 
    'Professional', 
    50, 
    10000, 
    100
);

-- Insert admin user (password: admin123)
INSERT INTO users (
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
    true, 
    true
);

-- Insert company admin user (password: company123)
INSERT INTO users (
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
    true, 
    true
);

-- Insert regular user (password: user123)
INSERT INTO users (
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
    true, 
    true
);