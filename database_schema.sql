CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;
CREATE TABLE "Companies" (
    "Id" uuid NOT NULL,
    "Name" character varying(255) NOT NULL,
    "Description" character varying(500),
    "Website" character varying(255),
    "ContactEmail" character varying(255),
    "ContactPhone" character varying(50),
    "Address" character varying(500),
    "IsActive" boolean NOT NULL,
    "MaxUsers" integer NOT NULL,
    "MaxDocuments" integer NOT NULL,
    "MaxStorageBytes" bigint NOT NULL,
    "MaxCollections" integer NOT NULL,
    "R2RApiKey" text,
    "R2RConfiguration" text,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    "CreatedBy" text,
    "UpdatedBy" text,
    CONSTRAINT "PK_Companies" PRIMARY KEY ("Id")
);

CREATE TABLE "IngestionJobs" (
    "Id" uuid NOT NULL,
    "FileName" text NOT NULL,
    "Status" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CompletedAt" timestamp with time zone,
    CONSTRAINT "PK_IngestionJobs" PRIMARY KEY ("Id")
);

CREATE TABLE "Users" (
    "Id" uuid NOT NULL,
    "Email" character varying(255) NOT NULL,
    "PasswordHash" character varying(255) NOT NULL,
    "FirstName" character varying(100) NOT NULL,
    "LastName" character varying(100) NOT NULL,
    "Role" integer NOT NULL,
    "CompanyId" uuid NOT NULL,
    "IsActive" boolean NOT NULL,
    "IsEmailVerified" boolean NOT NULL,
    "EmailVerificationToken" text,
    "EmailVerificationTokenExpiry" timestamp with time zone,
    "PasswordResetToken" text,
    "PasswordResetTokenExpiry" timestamp with time zone,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    "LastLoginAt" timestamp with time zone,
    "CreatedBy" text,
    "UpdatedBy" text,
    CONSTRAINT "PK_Users" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Users_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "AuditLogs" (
    "Id" uuid NOT NULL,
    "Action" character varying(100) NOT NULL,
    "EntityType" character varying(100) NOT NULL,
    "EntityId" uuid,
    "OldValues" character varying(1000),
    "NewValues" character varying(1000),
    "Details" character varying(500),
    "IpAddress" character varying(45),
    "UserAgent" character varying(500),
    "CompanyId" uuid NOT NULL,
    "UserId" uuid,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    CONSTRAINT "PK_AuditLogs" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AuditLogs_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_AuditLogs_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE SET NULL
);

CREATE TABLE "Collections" (
    "Id" uuid NOT NULL,
    "Name" character varying(255) NOT NULL,
    "Description" character varying(1000),
    "IsPublic" boolean NOT NULL,
    "R2RCollectionId" text,
    "CompanyId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    "CreatedBy" text,
    "UpdatedBy" text,
    CONSTRAINT "PK_Collections" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Collections_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Collections_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "Documents" (
    "Id" uuid NOT NULL,
    "FileName" character varying(255) NOT NULL,
    "OriginalFileName" character varying(255) NOT NULL,
    "ContentType" character varying(100) NOT NULL,
    "SizeBytes" bigint NOT NULL,
    "FilePath" character varying(500) NOT NULL,
    "FileHash" character varying(64),
    "Description" character varying(1000),
    "Status" integer NOT NULL,
    "StatusMessage" character varying(500),
    "R2RDocumentId" text,
    "R2RIngestionJobId" text,
    "R2RProcessedAt" timestamp with time zone,
    "CompanyId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    "CreatedBy" text,
    "UpdatedBy" text,
    CONSTRAINT "PK_Documents" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Documents_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Documents_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "CollectionDocuments" (
    "Id" uuid NOT NULL,
    "CollectionId" uuid NOT NULL,
    "DocumentId" uuid NOT NULL,
    "AddedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    "AddedBy" text,
    CONSTRAINT "PK_CollectionDocuments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CollectionDocuments_Collections_CollectionId" FOREIGN KEY ("CollectionId") REFERENCES "Collections" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_CollectionDocuments_Documents_DocumentId" FOREIGN KEY ("DocumentId") REFERENCES "Documents" ("Id") ON DELETE CASCADE
);

CREATE TABLE "DocumentChunks" (
    "Id" uuid NOT NULL,
    "DocumentId" uuid NOT NULL,
    "IngestionJobId" uuid,
    "Sequence" integer NOT NULL,
    "Content" text NOT NULL,
    "Data" text,
    "Metadata" text,
    "Status" integer NOT NULL,
    "R2RResult" text,
    "R2RChunkId" text,
    "R2RVectorId" text,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    CONSTRAINT "PK_DocumentChunks" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_DocumentChunks_Documents_DocumentId" FOREIGN KEY ("DocumentId") REFERENCES "Documents" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_DocumentChunks_IngestionJobs_IngestionJobId" FOREIGN KEY ("IngestionJobId") REFERENCES "IngestionJobs" ("Id")
);

CREATE INDEX "IX_AuditLogs_CompanyId_CreatedAt" ON "AuditLogs" ("CompanyId", "CreatedAt");

CREATE INDEX "IX_AuditLogs_EntityType_EntityId" ON "AuditLogs" ("EntityType", "EntityId");

CREATE INDEX "IX_AuditLogs_UserId" ON "AuditLogs" ("UserId");

CREATE UNIQUE INDEX "IX_CollectionDocuments_CollectionId_DocumentId" ON "CollectionDocuments" ("CollectionId", "DocumentId");

CREATE INDEX "IX_CollectionDocuments_DocumentId" ON "CollectionDocuments" ("DocumentId");

CREATE UNIQUE INDEX "IX_Collections_CompanyId_Name" ON "Collections" ("CompanyId", "Name");

CREATE INDEX "IX_Collections_R2RCollectionId" ON "Collections" ("R2RCollectionId");

CREATE INDEX "IX_Collections_UserId" ON "Collections" ("UserId");

CREATE UNIQUE INDEX "IX_Companies_ContactEmail" ON "Companies" ("ContactEmail");

CREATE UNIQUE INDEX "IX_Companies_Name" ON "Companies" ("Name");

CREATE UNIQUE INDEX "IX_DocumentChunks_DocumentId_Sequence" ON "DocumentChunks" ("DocumentId", "Sequence");

CREATE INDEX "IX_DocumentChunks_IngestionJobId" ON "DocumentChunks" ("IngestionJobId");

CREATE INDEX "IX_DocumentChunks_R2RChunkId" ON "DocumentChunks" ("R2RChunkId");

CREATE INDEX "IX_Documents_CompanyId_UserId" ON "Documents" ("CompanyId", "UserId");

CREATE INDEX "IX_Documents_FileHash" ON "Documents" ("FileHash");

CREATE INDEX "IX_Documents_R2RDocumentId" ON "Documents" ("R2RDocumentId");

CREATE INDEX "IX_Documents_UserId" ON "Documents" ("UserId");

CREATE UNIQUE INDEX "IX_Users_CompanyId_Email" ON "Users" ("CompanyId", "Email");

CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250612141222_InitialCreate', '9.0.6');

CREATE TABLE "UserDashboardWidgets" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "WidgetType" character varying(50) NOT NULL,
    "WidgetId" character varying(100) NOT NULL,
    "Title" character varying(200) NOT NULL,
    "Configuration" jsonb NOT NULL,
    "PositionX" integer NOT NULL,
    "PositionY" integer NOT NULL,
    "Width" integer NOT NULL,
    "Height" integer NOT NULL,
    "Order" integer NOT NULL,
    "IsVisible" boolean NOT NULL,
    "IsEnabled" boolean NOT NULL,
    "MinimumRole" integer NOT NULL,
    "RefreshInterval" integer NOT NULL,
    "Theme" character varying(20) NOT NULL,
    "CssClasses" character varying(500),
    "DataSource" character varying(500),
    "CacheTtl" integer NOT NULL,
    "SupportsExport" boolean NOT NULL,
    "SupportsClick" boolean NOT NULL,
    "ClickAction" character varying(500),
    "Description" character varying(1000),
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" character varying(100),
    "UpdatedBy" character varying(100),
    CONSTRAINT "PK_UserDashboardWidgets" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_UserDashboardWidgets_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_UserDashboardWidgets_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "WidgetTemplates" (
    "Id" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "WidgetType" character varying(50) NOT NULL,
    "Description" character varying(500),
    "DefaultConfiguration" jsonb NOT NULL,
    "DefaultWidth" integer NOT NULL,
    "DefaultHeight" integer NOT NULL,
    "MinimumRole" integer NOT NULL,
    "Category" character varying(50) NOT NULL,
    "Icon" character varying(50) NOT NULL,
    "IsActive" boolean NOT NULL,
    "Order" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_WidgetTemplates" PRIMARY KEY ("Id")
);

CREATE INDEX "IX_UserDashboardWidgets_CompanyId" ON "UserDashboardWidgets" ("CompanyId");

CREATE INDEX "IX_UserDashboardWidgets_UserId" ON "UserDashboardWidgets" ("UserId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250615073932_AddDashboardWidgets', '9.0.6');

ALTER TABLE "Documents" ADD "CollectionId" uuid;

ALTER TABLE "Documents" ADD "HasThumbnail" boolean NOT NULL DEFAULT FALSE;

ALTER TABLE "Documents" ADD "HasVersions" boolean NOT NULL DEFAULT FALSE;

ALTER TABLE "Documents" ADD "IsFavorite" boolean NOT NULL DEFAULT FALSE;

ALTER TABLE "Documents" ADD "IsProcessing" boolean NOT NULL DEFAULT FALSE;

ALTER TABLE "Documents" ADD "LastViewedAt" timestamp with time zone;

ALTER TABLE "Documents" ADD "Metadata" jsonb NOT NULL;

ALTER TABLE "Documents" ADD "Name" character varying(255) NOT NULL DEFAULT '';

ALTER TABLE "Documents" ADD "ProcessingError" text;

ALTER TABLE "Documents" ADD "ProcessingProgress" double precision;

ALTER TABLE "Documents" ADD "Size" bigint NOT NULL DEFAULT 0;

ALTER TABLE "Documents" ADD "Tags" jsonb NOT NULL;

ALTER TABLE "Documents" ADD "Version" text NOT NULL DEFAULT '';

ALTER TABLE "Documents" ADD "ViewCount" integer NOT NULL DEFAULT 0;

CREATE INDEX "IX_Documents_CollectionId" ON "Documents" ("CollectionId");

ALTER TABLE "Documents" ADD CONSTRAINT "FK_Documents_Collections_CollectionId" FOREIGN KEY ("CollectionId") REFERENCES "Collections" ("Id");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250616095305_AddNameColumnToDocuments', '9.0.6');

ALTER TABLE "Collections" ADD "Color" character varying(7) NOT NULL DEFAULT '';

ALTER TABLE "Collections" ADD "Icon" character varying(50) NOT NULL DEFAULT '';

ALTER TABLE "Collections" ADD "IsFavorite" boolean NOT NULL DEFAULT FALSE;

ALTER TABLE "Collections" ADD "TagsJson" text;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250616115426_AddCollectionUIFields', '9.0.6');

ALTER TABLE "Documents" DROP COLUMN "Size";

ALTER TABLE "Users" RENAME COLUMN "IsEmailVerified" TO "IsVerified";

ALTER TABLE "Documents" RENAME COLUMN "SizeBytes" TO "SizeInBytes";

ALTER TABLE "Users" ADD "Bio" character varying(1000);

ALTER TABLE "Users" ADD "Name" character varying(255);

ALTER TABLE "Users" ADD "ProfilePicture" character varying(500);

ALTER TABLE "Documents" ALTER COLUMN "Tags" TYPE TEXT;

ALTER TABLE "Documents" ALTER COLUMN "Metadata" TYPE TEXT;

ALTER TABLE "Documents" ADD "DocumentType" character varying(10);

ALTER TABLE "Documents" ADD "ExtractionStatus" character varying(50) NOT NULL DEFAULT '';

ALTER TABLE "Documents" ADD "IngestionStatus" character varying(50) NOT NULL DEFAULT '';

ALTER TABLE "Companies" ADD "TenantId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE "Collections" ADD "GraphClusterStatus" character varying(50);

ALTER TABLE "Collections" ADD "GraphSyncStatus" character varying(50);

ALTER TABLE "Collections" ADD "LastSyncedAt" timestamp with time zone;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250618140012_AddR2RCompatibilityFields', '9.0.6');

CREATE TABLE "RefreshTokens" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Token" character varying(512) NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    "RevokedAt" timestamp with time zone,
    "RevokedReason" character varying(100),
    CONSTRAINT "PK_RefreshTokens" PRIMARY KEY ("Id")
);

CREATE TABLE "TokenBlacklists" (
    "Id" uuid NOT NULL,
    "TokenHash" character varying(64) NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    "UserId" uuid,
    "Reason" character varying(50),
    CONSTRAINT "PK_TokenBlacklists" PRIMARY KEY ("Id")
);

CREATE INDEX "IX_RefreshTokens_ExpiresAt" ON "RefreshTokens" ("ExpiresAt");

CREATE UNIQUE INDEX "IX_RefreshTokens_Token" ON "RefreshTokens" ("Token");

CREATE INDEX "IX_RefreshTokens_UserId" ON "RefreshTokens" ("UserId");

CREATE INDEX "IX_RefreshTokens_UserId_ExpiresAt" ON "RefreshTokens" ("UserId", "ExpiresAt");

CREATE INDEX "IX_TokenBlacklists_ExpiresAt" ON "TokenBlacklists" ("ExpiresAt");

CREATE UNIQUE INDEX "IX_TokenBlacklists_TokenHash" ON "TokenBlacklists" ("TokenHash");

CREATE INDEX "IX_TokenBlacklists_UserId" ON "TokenBlacklists" ("UserId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250618155715_AddAuthTables', '9.0.6');

ALTER TABLE "Users" ADD "R2RUserId" character varying(255);

ALTER TABLE "Companies" ADD "R2RTenantId" character varying(255);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250618163720_AddR2RUserAndTenantIds', '9.0.6');

ALTER TABLE "Messages" ADD "UserId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE "Messages" ADD "IsEdited" boolean NOT NULL DEFAULT FALSE;

ALTER TABLE "Messages" ADD "OriginalContent" text;

ALTER TABLE "Messages" ADD "EditHistory" TEXT;

ALTER TABLE "Messages" ADD "LastEditedAt" timestamp with time zone;

ALTER TABLE "Messages" ADD "LastEditedByUserId" uuid;

ALTER TABLE "Messages" ADD "RagContext" TEXT;

ALTER TABLE "Messages" ADD "Status" character varying(50) NOT NULL DEFAULT 'sent';

ALTER TABLE "Messages" ADD "ErrorMessage" character varying(2000);

ALTER TABLE "Messages" ADD "ProcessingTimeMs" integer;

ALTER TABLE "Conversations" ADD "MessageCount" integer NOT NULL DEFAULT 0;

ALTER TABLE "Conversations" ADD "IsPinned" boolean NOT NULL DEFAULT FALSE;

ALTER TABLE "Conversations" ADD "Visibility" character varying(20) NOT NULL DEFAULT 'private';

ALTER TABLE "Conversations" ADD "SharedUserIds" TEXT;

ALTER TABLE "Conversations" ADD "Tags" TEXT;

ALTER TABLE "Conversations" ADD "Metadata" TEXT;

CREATE INDEX "IX_Messages_UserId" ON "Messages" ("UserId");

CREATE INDEX "IX_Messages_LastEditedByUserId" ON "Messages" ("LastEditedByUserId");

CREATE INDEX "IX_Messages_IsEdited" ON "Messages" ("IsEdited");

CREATE INDEX "IX_Messages_LastEditedAt" ON "Messages" ("LastEditedAt");

CREATE INDEX "IX_Conversations_Visibility" ON "Conversations" ("Visibility");

ALTER TABLE "Messages" ADD CONSTRAINT "FK_Messages_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT;

ALTER TABLE "Messages" ADD CONSTRAINT "FK_Messages_Users_LastEditedByUserId" FOREIGN KEY ("LastEditedByUserId") REFERENCES "Users" ("Id") ON DELETE SET NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250622080851_AddMissingChatFields', '9.0.6');

ALTER TABLE "Conversations" ALTER COLUMN "Settings" TYPE jsonb;

ALTER TABLE "Conversations" ALTER COLUMN "CollectionIds" TYPE jsonb;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250701114714_InitialCreateLatest', '9.0.6');

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250701114856_CreateChatTables', '9.0.6');

COMMIT;

