-- Create LLM Audit Tables for tracking configuration changes and usage
-- This enables comprehensive auditing of per-user LLM configurations

-- Check if LLMAuditLogs table already exists
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'LLMAuditLogs') THEN
        -- Create the LLMAuditLogs table
        CREATE TABLE "LLMAuditLogs" (
            "Id" uuid NOT NULL,
            "UserId" uuid NOT NULL,
            "Action" character varying(50) NOT NULL,
            "OldConfiguration" text,
            "NewConfiguration" text,
            "ChangedBy" character varying(255) NOT NULL,
            "Timestamp" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
            "IpAddress" character varying(45),
            "UserAgent" character varying(500),
            
            -- Primary key
            CONSTRAINT "PK_LLMAuditLogs" PRIMARY KEY ("Id"),
            
            -- Foreign key to Users table
            CONSTRAINT "FK_LLMAuditLogs_Users_UserId" 
                FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
        );

        -- Create indexes for performance
        CREATE INDEX "IX_LLMAuditLogs_UserId" ON "LLMAuditLogs" ("UserId");
        CREATE INDEX "IX_LLMAuditLogs_Action" ON "LLMAuditLogs" ("Action");
        CREATE INDEX "IX_LLMAuditLogs_Timestamp" ON "LLMAuditLogs" ("Timestamp");
        CREATE INDEX "IX_LLMAuditLogs_UserId_Timestamp" ON "LLMAuditLogs" ("UserId", "Timestamp");

        RAISE NOTICE 'LLMAuditLogs table created successfully with indexes';
    ELSE
        RAISE NOTICE 'LLMAuditLogs table already exists';
    END IF;
END $$;

-- Check if LLMUsageLogs table already exists
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'LLMUsageLogs') THEN
        -- Create the LLMUsageLogs table
        CREATE TABLE "LLMUsageLogs" (
            "Id" uuid NOT NULL,
            "UserId" uuid NOT NULL,
            "Provider" character varying(50) NOT NULL,
            "Model" character varying(100) NOT NULL,
            "Success" boolean NOT NULL,
            "ErrorMessage" text,
            "Timestamp" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
            "ResponseTimeMs" integer,
            
            -- Primary key
            CONSTRAINT "PK_LLMUsageLogs" PRIMARY KEY ("Id"),
            
            -- Foreign key to Users table
            CONSTRAINT "FK_LLMUsageLogs_Users_UserId" 
                FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
        );

        -- Create indexes for performance
        CREATE INDEX "IX_LLMUsageLogs_UserId" ON "LLMUsageLogs" ("UserId");
        CREATE INDEX "IX_LLMUsageLogs_Provider" ON "LLMUsageLogs" ("Provider");
        CREATE INDEX "IX_LLMUsageLogs_Success" ON "LLMUsageLogs" ("Success");
        CREATE INDEX "IX_LLMUsageLogs_Timestamp" ON "LLMUsageLogs" ("Timestamp");
        CREATE INDEX "IX_LLMUsageLogs_UserId_Provider_Timestamp" ON "LLMUsageLogs" ("UserId", "Provider", "Timestamp");

        RAISE NOTICE 'LLMUsageLogs table created successfully with indexes';
    ELSE
        RAISE NOTICE 'LLMUsageLogs table already exists';
    END IF;
END $$;

-- Mark the migration as applied in EF Core migration history
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250709190504_AddLLMAuditTables', '9.0.6')
ON CONFLICT ("MigrationId") DO NOTHING;

-- Insert sample audit log entry for testing
INSERT INTO "LLMAuditLogs" (
    "Id", 
    "UserId", 
    "Action", 
    "NewConfiguration", 
    "ChangedBy", 
    "Timestamp"
)
SELECT 
    gen_random_uuid(),
    "Id",
    'CREATE',
    '{"provider":"openai","model":"gpt-4o-mini","temperature":0.7,"maxTokens":1000}',
    'system',
    CURRENT_TIMESTAMP
FROM "Users" 
WHERE "Email" = 'r.antoniucci@microsis.it'
LIMIT 1
ON CONFLICT DO NOTHING;

-- Verify the tables were created
SELECT 
    table_name,
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns 
WHERE table_name IN ('LLMAuditLogs', 'LLMUsageLogs')
ORDER BY table_name, ordinal_position;
