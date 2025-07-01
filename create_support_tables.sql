-- Create supporting tables and indexes

-- Create AuditLogs table
CREATE TABLE "AuditLogs" (
    "Id" uuid NOT NULL,
    "Action" character varying(100) NOT NULL,
    "EntityType" character varying(100) NOT NULL,
    "EntityId" uuid,
    "OldValues" character varying(1000),
    "NewValues" character varying(1000),
    "UserId" uuid,
    "Timestamp" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "PK_AuditLogs" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AuditLogs_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE SET NULL
);

-- Create RefreshTokens table
CREATE TABLE "RefreshTokens" (
    "Id" uuid NOT NULL,
    "Token" character varying(255) NOT NULL,
    "UserId" uuid NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "RevokedAt" timestamp with time zone,
    CONSTRAINT "PK_RefreshTokens" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_RefreshTokens_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

-- Create TokenBlacklists table
CREATE TABLE "TokenBlacklists" (
    "Id" uuid NOT NULL,
    "TokenHash" character varying(255) NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "PK_TokenBlacklists" PRIMARY KEY ("Id")
);

-- Create indexes for performance
CREATE INDEX "IX_Users_CompanyId" ON "Users" ("CompanyId");
CREATE INDEX "IX_Users_Email" ON "Users" ("Email");
CREATE INDEX "IX_Users_R2RUserId" ON "Users" ("R2RUserId");
CREATE INDEX "IX_Collections_CompanyId" ON "Collections" ("CompanyId");
CREATE INDEX "IX_Collections_UserId" ON "Collections" ("UserId");
CREATE INDEX "IX_Collections_R2RCollectionId" ON "Collections" ("R2RCollectionId");
CREATE INDEX "IX_Documents_CollectionId" ON "Documents" ("CollectionId");
CREATE INDEX "IX_Documents_CompanyId" ON "Documents" ("CompanyId");
CREATE INDEX "IX_Documents_UserId" ON "Documents" ("UserId");
CREATE INDEX "IX_Documents_R2RDocumentId" ON "Documents" ("R2RDocumentId");
CREATE INDEX "IX_Documents_R2RIngestionJobId" ON "Documents" ("R2RIngestionJobId");
CREATE INDEX "IX_AuditLogs_UserId" ON "AuditLogs" ("UserId");
CREATE INDEX "IX_RefreshTokens_UserId" ON "RefreshTokens" ("UserId");
CREATE INDEX "IX_TokenBlacklists_TokenHash" ON "TokenBlacklists" ("TokenHash");

-- Insert migration history for core tables
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES ('20250612141222_InitialCreate', '9.0.6');
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES ('20250612141223_AddUserFields', '9.0.6');
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES ('20250612141224_AddDocumentFields', '9.0.6');
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES ('20250612141225_AddCollectionFields', '9.0.6');
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES ('20250612141226_AddR2RFields', '9.0.6');
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES ('20250612141227_AddSecurityTables', '9.0.6');
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES ('20250618163720_AddR2RUserAndTenantIds', '9.0.6');
