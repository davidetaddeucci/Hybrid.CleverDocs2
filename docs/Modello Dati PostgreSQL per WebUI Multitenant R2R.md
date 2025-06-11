# Modello Dati PostgreSQL per WebUI Multitenant R2R

## Panoramica

Questo documento descrive il modello dati PostgreSQL per il sistema WebUI multitenant di SciPhi AI R2R. Il database è progettato per supportare la multitenancy, la gestione di collezioni di documenti, l'autenticazione e autorizzazione basata su ruoli, e l'integrazione con il sistema R2R API.

## Schema Database

Il database utilizza uno schema multitenancy basato su discriminatore, dove ogni entità contiene un riferimento alla Company di appartenenza. Questo approccio consente di mantenere tutti i dati in un unico database fisico, facilitando query cross-tenant quando necessario, ma garantendo al contempo l'isolamento dei dati.

### Diagramma ER

```
┌───────────────┐       ┌───────────────┐       ┌───────────────┐
│   Companies   │       │     Users     │       │     Roles     │
├───────────────┤       ├───────────────┤       ├───────────────┤
│ id            │       │ id            │       │ id            │
│ name          │◄──────┤ company_id    │       │ name          │
│ logo_url      │       │ username      │       │ description   │
│ is_active     │       │ email         │       └───────┬───────┘
│ created_at    │       │ password_hash │               │
│ updated_at    │       │ first_name    │       ┌───────┴───────┐
└───────────────┘       │ last_name     │       │  UserRoles    │
        │               │ is_active     │       ├───────────────┤
        │               │ created_at    │       │ user_id       │
        │               │ updated_at    │◄──────┤ role_id       │
        │               └───────┬───────┘       └───────────────┘
        │                       │
        │                       │
┌───────┴───────┐       ┌───────┴───────┐       ┌───────────────┐
│CompanySettings │       │ Collections   │       │   Documents   │
├───────────────┤       ├───────────────┤       ├───────────────┤
│ id            │       │ id            │       │ id            │
│ company_id    │       │ company_id    │◄──────┤ collection_id │
│ setting_key   │       │ user_id       │       │ r2r_document_id│
│ setting_value │       │ name          │       │ file_name     │
│ created_at    │       │ description   │       │ file_size     │
│ updated_at    │       │ created_at    │       │ mime_type     │
└───────────────┘       │ updated_at    │       │ status        │
                        └───────────────┘       │ created_at    │
                                                │ updated_at    │
                                                └───────┬───────┘
                                                        │
┌───────────────┐       ┌───────────────┐       ┌───────┴───────┐
│Conversations  │       │   Messages    │       │DocumentMetadata│
├───────────────┤       ├───────────────┤       ├───────────────┤
│ id            │       │ id            │       │ id            │
│ user_id       │◄──────┤ conversation_id│      │ document_id   │
│ title         │       │ role          │       │ key           │
│ created_at    │       │ content       │       │ value         │
│ updated_at    │       │ created_at    │       │ created_at    │
└───────┬───────┘       └───────────────┘       └───────────────┘
        │
┌───────┴───────┐
│ConversationCol│
├───────────────┤
│conversation_id│
│collection_id  │
└───────────────┘
```

## Definizione delle Tabelle

### Companies

Tabella che rappresenta le aziende/tenant nel sistema.

```sql
CREATE TABLE companies (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    logo_url VARCHAR(255),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_companies_is_active ON companies(is_active);
```

### CompanySettings

Tabella per le impostazioni specifiche di ciascuna company.

```sql
CREATE TABLE company_settings (
    id SERIAL PRIMARY KEY,
    company_id INTEGER NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    setting_key VARCHAR(50) NOT NULL,
    setting_value TEXT,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uk_company_setting UNIQUE (company_id, setting_key)
);

CREATE INDEX idx_company_settings_company_id ON company_settings(company_id);
```

### Roles

Tabella per i ruoli di sistema.

```sql
CREATE TABLE roles (
    id SERIAL PRIMARY KEY,
    name VARCHAR(50) NOT NULL,
    description VARCHAR(255),
    CONSTRAINT uk_role_name UNIQUE (name)
);

-- Inserimento ruoli predefiniti
INSERT INTO roles (name, description) VALUES 
('Admin', 'Amministratore di sistema'),
('Company', 'Amministratore aziendale'),
('User', 'Utente standard');
```

### Users

Tabella per gli utenti del sistema.

```sql
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    company_id INTEGER NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    username VARCHAR(50) NOT NULL,
    email VARCHAR(100) NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    first_name VARCHAR(50) NOT NULL,
    last_name VARCHAR(50) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uk_user_username UNIQUE (username),
    CONSTRAINT uk_user_email UNIQUE (email)
);

CREATE INDEX idx_users_company_id ON users(company_id);
CREATE INDEX idx_users_is_active ON users(is_active);
```

### UserRoles

Tabella di associazione tra utenti e ruoli.

```sql
CREATE TABLE user_roles (
    user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    role_id INTEGER NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    PRIMARY KEY (user_id, role_id)
);

CREATE INDEX idx_user_roles_user_id ON user_roles(user_id);
CREATE INDEX idx_user_roles_role_id ON user_roles(role_id);
```

### Collections

Tabella per le collezioni di documenti.

```sql
CREATE TABLE collections (
    id SERIAL PRIMARY KEY,
    company_id INTEGER NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_collections_company_id ON collections(company_id);
CREATE INDEX idx_collections_user_id ON collections(user_id);
```

### Documents

Tabella per i documenti caricati nel sistema.

```sql
CREATE TABLE documents (
    id SERIAL PRIMARY KEY,
    collection_id INTEGER NOT NULL REFERENCES collections(id) ON DELETE CASCADE,
    r2r_document_id VARCHAR(100) NOT NULL,
    file_name VARCHAR(255) NOT NULL,
    file_size BIGINT NOT NULL,
    mime_type VARCHAR(100) NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'pending',
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uk_document_r2r_id UNIQUE (r2r_document_id)
);

CREATE INDEX idx_documents_collection_id ON documents(collection_id);
CREATE INDEX idx_documents_status ON documents(status);
```

### DocumentMetadata

Tabella per i metadati dei documenti.

```sql
CREATE TABLE document_metadata (
    id SERIAL PRIMARY KEY,
    document_id INTEGER NOT NULL REFERENCES documents(id) ON DELETE CASCADE,
    key VARCHAR(50) NOT NULL,
    value TEXT,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uk_document_metadata UNIQUE (document_id, key)
);

CREATE INDEX idx_document_metadata_document_id ON document_metadata(document_id);
```

### Conversations

Tabella per le conversazioni con il chatbot.

```sql
CREATE TABLE conversations (
    id SERIAL PRIMARY KEY,
    user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    title VARCHAR(255) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_conversations_user_id ON conversations(user_id);
```

### ConversationCollections

Tabella di associazione tra conversazioni e collezioni.

```sql
CREATE TABLE conversation_collections (
    conversation_id INTEGER NOT NULL REFERENCES conversations(id) ON DELETE CASCADE,
    collection_id INTEGER NOT NULL REFERENCES collections(id) ON DELETE CASCADE,
    PRIMARY KEY (conversation_id, collection_id)
);

CREATE INDEX idx_conversation_collections_conversation_id ON conversation_collections(conversation_id);
CREATE INDEX idx_conversation_collections_collection_id ON conversation_collections(collection_id);
```

### Messages

Tabella per i messaggi nelle conversazioni.

```sql
CREATE TABLE messages (
    id SERIAL PRIMARY KEY,
    conversation_id INTEGER NOT NULL REFERENCES conversations(id) ON DELETE CASCADE,
    role VARCHAR(20) NOT NULL,
    content TEXT NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_messages_conversation_id ON messages(conversation_id);
```

### ApiKeys

Tabella per le API key utilizzate per l'integrazione con R2R.

```sql
CREATE TABLE api_keys (
    id SERIAL PRIMARY KEY,
    company_id INTEGER NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    key_name VARCHAR(50) NOT NULL,
    key_value VARCHAR(255) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uk_api_key_company UNIQUE (company_id, key_name)
);

CREATE INDEX idx_api_keys_company_id ON api_keys(company_id);
CREATE INDEX idx_api_keys_is_active ON api_keys(is_active);
```

### QueueItems

Tabella per tracciare gli elementi in coda (backup/audit delle operazioni RabbitMQ).

```sql
CREATE TABLE queue_items (
    id SERIAL PRIMARY KEY,
    queue_name VARCHAR(50) NOT NULL,
    item_type VARCHAR(100) NOT NULL,
    item_data JSONB NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'pending',
    priority INTEGER NOT NULL DEFAULT 0,
    company_id INTEGER NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    retry_count INTEGER NOT NULL DEFAULT 0,
    max_retries INTEGER NOT NULL DEFAULT 3,
    next_retry_at TIMESTAMP WITH TIME ZONE,
    error_message TEXT,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    processing_started_at TIMESTAMP WITH TIME ZONE,
    completed_at TIMESTAMP WITH TIME ZONE
);

CREATE INDEX idx_queue_items_status ON queue_items(status);
CREATE INDEX idx_queue_items_queue_name ON queue_items(queue_name);
CREATE INDEX idx_queue_items_company_id ON queue_items(company_id);
CREATE INDEX idx_queue_items_next_retry_at ON queue_items(next_retry_at);
```

### SystemSettings

Tabella per le impostazioni globali del sistema.

```sql
CREATE TABLE system_settings (
    id SERIAL PRIMARY KEY,
    setting_key VARCHAR(50) NOT NULL,
    setting_value TEXT,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uk_system_setting UNIQUE (setting_key)
);
```

### AuditLogs

Tabella per il logging di audit.

```sql
CREATE TABLE audit_logs (
    id SERIAL PRIMARY KEY,
    user_id INTEGER REFERENCES users(id) ON DELETE SET NULL,
    company_id INTEGER REFERENCES companies(id) ON DELETE SET NULL,
    action VARCHAR(100) NOT NULL,
    entity_type VARCHAR(50),
    entity_id VARCHAR(50),
    old_values JSONB,
    new_values JSONB,
    ip_address VARCHAR(45),
    user_agent VARCHAR(255),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_audit_logs_user_id ON audit_logs(user_id);
CREATE INDEX idx_audit_logs_company_id ON audit_logs(company_id);
CREATE INDEX idx_audit_logs_action ON audit_logs(action);
CREATE INDEX idx_audit_logs_created_at ON audit_logs(created_at);
```

## Funzioni e Trigger

### Aggiornamento Timestamp

```sql
CREATE OR REPLACE FUNCTION update_timestamp()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Applicazione trigger a tutte le tabelle con updated_at
CREATE TRIGGER update_companies_timestamp
BEFORE UPDATE ON companies
FOR EACH ROW EXECUTE FUNCTION update_timestamp();

CREATE TRIGGER update_company_settings_timestamp
BEFORE UPDATE ON company_settings
FOR EACH ROW EXECUTE FUNCTION update_timestamp();

CREATE TRIGGER update_users_timestamp
BEFORE UPDATE ON users
FOR EACH ROW EXECUTE FUNCTION update_timestamp();

CREATE TRIGGER update_collections_timestamp
BEFORE UPDATE ON collections
FOR EACH ROW EXECUTE FUNCTION update_timestamp();

CREATE TRIGGER update_documents_timestamp
BEFORE UPDATE ON documents
FOR EACH ROW EXECUTE FUNCTION update_timestamp();

CREATE TRIGGER update_conversations_timestamp
BEFORE UPDATE ON conversations
FOR EACH ROW EXECUTE FUNCTION update_timestamp();

CREATE TRIGGER update_api_keys_timestamp
BEFORE UPDATE ON api_keys
FOR EACH ROW EXECUTE FUNCTION update_timestamp();

CREATE TRIGGER update_system_settings_timestamp
BEFORE UPDATE ON system_settings
FOR EACH ROW EXECUTE FUNCTION update_timestamp();
```

### Audit Logging

```sql
CREATE OR REPLACE FUNCTION audit_log_changes()
RETURNS TRIGGER AS $$
DECLARE
    user_id INTEGER;
    company_id INTEGER;
BEGIN
    -- Ottieni user_id e company_id dal contesto applicativo
    user_id := current_setting('app.current_user_id', true)::INTEGER;
    company_id := current_setting('app.current_company_id', true)::INTEGER;
    
    IF TG_OP = 'INSERT' THEN
        INSERT INTO audit_logs (
            user_id, company_id, action, entity_type, entity_id, 
            new_values, ip_address, user_agent
        ) VALUES (
            user_id, company_id, 'INSERT', TG_TABLE_NAME, NEW.id::TEXT,
            row_to_json(NEW), current_setting('app.client_ip', true), 
            current_setting('app.user_agent', true)
        );
        RETURN NEW;
    ELSIF TG_OP = 'UPDATE' THEN
        INSERT INTO audit_logs (
            user_id, company_id, action, entity_type, entity_id, 
            old_values, new_values, ip_address, user_agent
        ) VALUES (
            user_id, company_id, 'UPDATE', TG_TABLE_NAME, NEW.id::TEXT,
            row_to_json(OLD), row_to_json(NEW), 
            current_setting('app.client_ip', true), 
            current_setting('app.user_agent', true)
        );
        RETURN NEW;
    ELSIF TG_OP = 'DELETE' THEN
        INSERT INTO audit_logs (
            user_id, company_id, action, entity_type, entity_id, 
            old_values, ip_address, user_agent
        ) VALUES (
            user_id, company_id, 'DELETE', TG_TABLE_NAME, OLD.id::TEXT,
            row_to_json(OLD), current_setting('app.client_ip', true), 
            current_setting('app.user_agent', true)
        );
        RETURN OLD;
    END IF;
    RETURN NULL;
END;
$$ LANGUAGE plpgsql;

-- Applicazione trigger di audit alle tabelle principali
CREATE TRIGGER audit_companies
AFTER INSERT OR UPDATE OR DELETE ON companies
FOR EACH ROW EXECUTE FUNCTION audit_log_changes();

CREATE TRIGGER audit_users
AFTER INSERT OR UPDATE OR DELETE ON users
FOR EACH ROW EXECUTE FUNCTION audit_log_changes();

CREATE TRIGGER audit_collections
AFTER INSERT OR UPDATE OR DELETE ON collections
FOR EACH ROW EXECUTE FUNCTION audit_log_changes();

CREATE TRIGGER audit_documents
AFTER INSERT OR UPDATE OR DELETE ON documents
FOR EACH ROW EXECUTE FUNCTION audit_log_changes();
```

## Viste

### UserCollectionsView

Vista per visualizzare le collezioni accessibili a ciascun utente.

```sql
CREATE OR REPLACE VIEW user_collections_view AS
SELECT 
    c.id,
    c.name,
    c.description,
    c.company_id,
    c.user_id,
    u.username as owner_username,
    c.created_at,
    c.updated_at,
    COUNT(d.id) as document_count
FROM 
    collections c
LEFT JOIN 
    users u ON c.user_id = u.id
LEFT JOIN 
    documents d ON c.id = d.collection_id
GROUP BY 
    c.id, c.name, c.description, c.company_id, c.user_id, u.username, c.created_at, c.updated_at;
```

### CompanyStatsView

Vista per statistiche aggregate per company.

```sql
CREATE OR REPLACE VIEW company_stats_view AS
SELECT 
    c.id as company_id,
    c.name as company_name,
    COUNT(DISTINCT u.id) as user_count,
    COUNT(DISTINCT col.id) as collection_count,
    COUNT(DISTINCT d.id) as document_count,
    COUNT(DISTINCT conv.id) as conversation_count,
    COUNT(DISTINCT m.id) as message_count,
    MAX(d.created_at) as last_document_date,
    MAX(m.created_at) as last_message_date
FROM 
    companies c
LEFT JOIN 
    users u ON c.id = u.company_id
LEFT JOIN 
    collections col ON c.id = col.company_id
LEFT JOIN 
    documents d ON col.id = d.collection_id
LEFT JOIN 
    conversations conv ON u.id = conv.user_id
LEFT JOIN 
    messages m ON conv.id = m.conversation_id
WHERE 
    c.is_active = true
GROUP BY 
    c.id, c.name;
```

## Indici e Ottimizzazioni

Oltre agli indici già definiti nelle tabelle, ecco alcune ottimizzazioni aggiuntive:

### Indici Composti

```sql
-- Indice per ricerche di documenti per company
CREATE INDEX idx_documents_by_company ON documents
USING btree (collection_id)
INCLUDE (status, created_at)
WHERE status = 'completed';

-- Indice per ricerche di messaggi recenti
CREATE INDEX idx_recent_messages ON messages
USING btree (conversation_id, created_at DESC);

-- Indice per ricerche di collezioni per company e utente
CREATE INDEX idx_collections_company_user ON collections
USING btree (company_id, user_id);
```

### Partizioni

Per tabelle che crescono molto nel tempo, come `messages` e `audit_logs`, è consigliabile utilizzare il partizionamento:

```sql
-- Esempio di partizionamento per audit_logs
CREATE TABLE audit_logs_partitioned (
    id SERIAL,
    user_id INTEGER REFERENCES users(id) ON DELETE SET NULL,
    company_id INTEGER REFERENCES companies(id) ON DELETE SET NULL,
    action VARCHAR(100) NOT NULL,
    entity_type VARCHAR(50),
    entity_id VARCHAR(50),
    old_values JSONB,
    new_values JSONB,
    ip_address VARCHAR(45),
    user_agent VARCHAR(255),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
) PARTITION BY RANGE (created_at);

-- Creazione partizioni mensili
CREATE TABLE audit_logs_y2025m01 PARTITION OF audit_logs_partitioned
    FOR VALUES FROM ('2025-01-01') TO ('2025-02-01');
    
CREATE TABLE audit_logs_y2025m02 PARTITION OF audit_logs_partitioned
    FOR VALUES FROM ('2025-02-01') TO ('2025-03-01');
    
-- Continua per i mesi successivi...
```

## Considerazioni di Sicurezza

### Crittografia

Per dati sensibili come API key, è consigliabile utilizzare crittografia a livello di applicazione o la funzionalità pgcrypto:

```sql
-- Installazione estensione pgcrypto
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- Esempio di funzione per crittografare/decrittografare
CREATE OR REPLACE FUNCTION encrypt_api_key(p_key_value TEXT, p_secret TEXT)
RETURNS TEXT AS $$
BEGIN
    RETURN encode(encrypt(p_key_value::bytea, p_secret::bytea, 'aes'), 'base64');
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION decrypt_api_key(p_encrypted_value TEXT, p_secret TEXT)
RETURNS TEXT AS $$
BEGIN
    RETURN convert_from(decrypt(decode(p_encrypted_value, 'base64'), p_secret::bytea, 'aes'), 'utf8');
END;
$$ LANGUAGE plpgsql;
```

### Row Level Security

Per garantire l'isolamento dei dati tra tenant, è possibile utilizzare Row Level Security:

```sql
-- Abilitazione RLS per tabelle critiche
ALTER TABLE users ENABLE ROW LEVEL SECURITY;
ALTER TABLE collections ENABLE ROW LEVEL SECURITY;
ALTER TABLE documents ENABLE ROW LEVEL SECURITY;
ALTER TABLE conversations ENABLE ROW LEVEL SECURITY;
ALTER TABLE messages ENABLE ROW LEVEL SECURITY;

-- Policy per utenti Admin (accesso a tutti i dati)
CREATE POLICY admin_all_access ON users
    USING (pg_has_role(current_user, 'admin', 'member'));
    
-- Policy per utenti Company (accesso solo ai dati della propria company)
CREATE POLICY company_data_access ON users
    USING (company_id = current_setting('app.current_company_id')::INTEGER);
    
-- Policy per utenti standard (accesso solo ai propri dati)
CREATE POLICY user_data_access ON conversations
    USING (user_id = current_setting('app.current_user_id')::INTEGER);
```

## Migrazioni e Gestione Schema

Per gestire le migrazioni del database, è consigliabile utilizzare strumenti come Flyway o Liquibase. Ecco un esempio di struttura di migrazioni con Flyway:

```
db/migrations/
├── V1__Initial_Schema.sql
├── V2__Add_Audit_Logging.sql
├── V3__Add_Document_Metadata.sql
└── V4__Optimize_Indexes.sql
```

## Conclusioni

Il modello dati PostgreSQL presentato è progettato per supportare un'applicazione WebUI multitenant per SciPhi AI R2R, con particolare attenzione alla scalabilità, sicurezza e isolamento dei dati. L'utilizzo di PostgreSQL offre vantaggi in termini di funzionalità avanzate come JSONB per dati semi-strutturati, partizionamento per tabelle di grandi dimensioni, e Row Level Security per l'isolamento dei dati.

Il modello supporta tutti i requisiti funzionali del sistema, inclusi:
- Gestione multitenancy con Companies e Users
- Gestione di collezioni e documenti
- Integrazione con R2R API
- Conversazioni chatbot
- Audit logging completo
- Configurazioni specifiche per tenant

Questo schema può essere facilmente esteso per supportare requisiti aggiuntivi man mano che il sistema evolve.
