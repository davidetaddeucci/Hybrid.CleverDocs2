-- Create Conversations and Messages tables based on Entity Framework models
-- This script creates the tables with all columns, indexes, and foreign keys

-- Create Conversations table
CREATE TABLE "Conversations" (
    "Id" SERIAL PRIMARY KEY,
    "R2RConversationId" character varying(255) NOT NULL,
    "Title" character varying(500) NOT NULL,
    "Description" character varying(2000),
    "UserId" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CollectionIds" jsonb NOT NULL DEFAULT '[]',
    "Status" character varying(50) NOT NULL DEFAULT 'active',
    "Metadata" jsonb,
    "Settings" jsonb,
    "LastMessageAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "MessageCount" integer NOT NULL DEFAULT 0,
    "IsPinned" boolean NOT NULL DEFAULT FALSE,
    "Visibility" character varying(20) NOT NULL DEFAULT 'private',
    "SharedUserIds" jsonb,
    "Tags" jsonb,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create Messages table
CREATE TABLE "Messages" (
    "Id" SERIAL PRIMARY KEY,
    "R2RMessageId" character varying(255),
    "ConversationId" integer NOT NULL,
    "UserId" uuid NOT NULL,
    "Role" character varying(50) NOT NULL,
    "Content" text NOT NULL,
    "ParentMessageId" integer,
    "Metadata" jsonb,
    "Citations" jsonb,
    "IsEdited" boolean NOT NULL DEFAULT FALSE,
    "OriginalContent" text,
    "EditHistory" jsonb,
    "LastEditedAt" timestamp with time zone,
    "LastEditedByUserId" uuid,
    "RagContext" jsonb,
    "ConfidenceScore" double precision,
    "ProcessingTimeMs" integer,
    "TokenCount" integer,
    "Status" character varying(50) NOT NULL DEFAULT 'sent',
    "ErrorMessage" character varying(2000),
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes for Conversations table
CREATE UNIQUE INDEX "IX_Conversations_R2RConversationId" ON "Conversations" ("R2RConversationId");
CREATE INDEX "IX_Conversations_CompanyId_UserId" ON "Conversations" ("CompanyId", "UserId");
CREATE INDEX "IX_Conversations_UserId_LastMessageAt" ON "Conversations" ("UserId", "LastMessageAt");
CREATE INDEX "IX_Conversations_Status" ON "Conversations" ("Status");
CREATE INDEX "IX_Conversations_Visibility" ON "Conversations" ("Visibility");

-- Create indexes for Messages table
CREATE INDEX "IX_Messages_R2RMessageId" ON "Messages" ("R2RMessageId");
CREATE INDEX "IX_Messages_ConversationId_CreatedAt" ON "Messages" ("ConversationId", "CreatedAt");
CREATE INDEX "IX_Messages_ParentMessageId" ON "Messages" ("ParentMessageId");
CREATE INDEX "IX_Messages_Role" ON "Messages" ("Role");
CREATE INDEX "IX_Messages_Status" ON "Messages" ("Status");
CREATE INDEX "IX_Messages_IsEdited" ON "Messages" ("IsEdited");
CREATE INDEX "IX_Messages_LastEditedAt" ON "Messages" ("LastEditedAt");
CREATE INDEX "IX_Messages_UserId" ON "Messages" ("UserId");
CREATE INDEX "IX_Messages_LastEditedByUserId" ON "Messages" ("LastEditedByUserId");

-- Create foreign key constraints for Conversations table
ALTER TABLE "Conversations" ADD CONSTRAINT "FK_Conversations_Users_UserId" 
    FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT;

ALTER TABLE "Conversations" ADD CONSTRAINT "FK_Conversations_Companies_CompanyId" 
    FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE RESTRICT;

-- Create foreign key constraints for Messages table
ALTER TABLE "Messages" ADD CONSTRAINT "FK_Messages_Conversations_ConversationId" 
    FOREIGN KEY ("ConversationId") REFERENCES "Conversations" ("Id") ON DELETE CASCADE;

ALTER TABLE "Messages" ADD CONSTRAINT "FK_Messages_Users_UserId" 
    FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT;

ALTER TABLE "Messages" ADD CONSTRAINT "FK_Messages_Messages_ParentMessageId" 
    FOREIGN KEY ("ParentMessageId") REFERENCES "Messages" ("Id") ON DELETE RESTRICT;

ALTER TABLE "Messages" ADD CONSTRAINT "FK_Messages_Users_LastEditedByUserId" 
    FOREIGN KEY ("LastEditedByUserId") REFERENCES "Users" ("Id") ON DELETE SET NULL;

-- Mark the missing migration as applied
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250620122527_AddConversationAndMessageEntities', '9.0.6')
ON CONFLICT ("MigrationId") DO NOTHING;
