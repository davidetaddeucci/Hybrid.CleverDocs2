-- Create UserLLMPreferences table for per-user LLM configuration
-- This enables users to select their own LLM providers and use personal API keys

-- Check if table already exists
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'UserLLMPreferences') THEN
        -- Create the UserLLMPreferences table
        CREATE TABLE "UserLLMPreferences" (
            "UserId" uuid NOT NULL,
            "Provider" character varying(50) NOT NULL,
            "Model" character varying(100) NOT NULL,
            "ApiEndpoint" character varying(500),
            "EncryptedApiKey" character varying(1000),
            "Temperature" numeric(3,2) NOT NULL DEFAULT 0.7,
            "MaxTokens" integer NOT NULL DEFAULT 1000,
            "TopP" numeric(3,2) DEFAULT 1.0,
            "EnableStreaming" boolean NOT NULL DEFAULT false,
            "IsActive" boolean NOT NULL DEFAULT true,
            "AdditionalParameters" jsonb,
            "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
            "UpdatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
            "CreatedBy" character varying(255),
            "UpdatedBy" character varying(255),
            "LastUsedAt" timestamp with time zone,
            "UsageCount" integer NOT NULL DEFAULT 0,
            
            -- Primary key
            CONSTRAINT "PK_UserLLMPreferences" PRIMARY KEY ("UserId"),
            
            -- Foreign key to Users table
            CONSTRAINT "FK_UserLLMPreferences_Users_UserId" 
                FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE,
            
            -- Check constraints for validation
            CONSTRAINT "CK_UserLLMPreferences_Temperature" 
                CHECK ("Temperature" >= 0.0 AND "Temperature" <= 2.0),
            CONSTRAINT "CK_UserLLMPreferences_MaxTokens" 
                CHECK ("MaxTokens" >= 1 AND "MaxTokens" <= 32000),
            CONSTRAINT "CK_UserLLMPreferences_TopP" 
                CHECK ("TopP" IS NULL OR ("TopP" >= 0.0 AND "TopP" <= 1.0)),
            CONSTRAINT "CK_UserLLMPreferences_Provider" 
                CHECK ("Provider" IN ('openai', 'anthropic', 'azure', 'custom'))
        );

        -- Create indexes for performance
        CREATE INDEX "IX_UserLLMPreferences_Provider" ON "UserLLMPreferences" ("Provider");
        CREATE INDEX "IX_UserLLMPreferences_IsActive" ON "UserLLMPreferences" ("IsActive");
        CREATE INDEX "IX_UserLLMPreferences_Provider_IsActive" ON "UserLLMPreferences" ("Provider", "IsActive");
        CREATE INDEX "IX_UserLLMPreferences_LastUsedAt" ON "UserLLMPreferences" ("LastUsedAt");

        -- Insert sample data for testing (optional)
        -- This creates a default OpenAI configuration for the first user
        INSERT INTO "UserLLMPreferences" (
            "UserId", 
            "Provider", 
            "Model", 
            "Temperature", 
            "MaxTokens", 
            "IsActive",
            "CreatedBy"
        )
        SELECT 
            "Id",
            'openai',
            'gpt-4o-mini',
            0.7,
            1000,
            true,
            'system'
        FROM "Users" 
        WHERE "Email" = 'r.antoniucci@microsis.it'
        LIMIT 1
        ON CONFLICT ("UserId") DO NOTHING;

        RAISE NOTICE 'UserLLMPreferences table created successfully with indexes and constraints';
    ELSE
        RAISE NOTICE 'UserLLMPreferences table already exists';
    END IF;
END $$;

-- Mark the migration as applied in EF Core migration history
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250709162822_AddUserLLMPreferences', '9.0.6')
ON CONFLICT ("MigrationId") DO NOTHING;

-- Verify the table was created
SELECT 
    table_name,
    column_name,
    data_type,
    is_nullable,
    column_default
FROM information_schema.columns 
WHERE table_name = 'UserLLMPreferences'
ORDER BY ordinal_position;
