-- Drop and recreate migrations history
DROP TABLE IF EXISTS "__EFMigrationsHistory";

CREATE TABLE "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

-- Create Companies table
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
    "R2RTenantId" character varying(255),
    "TenantId" character varying(255),
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CreatedBy" text,
    "UpdatedBy" text,
    CONSTRAINT "PK_Companies" PRIMARY KEY ("Id")
);

-- Create Users table
CREATE TABLE "Users" (
    "Id" uuid NOT NULL,
    "Email" character varying(255) NOT NULL,
    "PasswordHash" character varying(255) NOT NULL,
    "FirstName" character varying(100) NOT NULL,
    "LastName" character varying(100) NOT NULL,
    "Name" character varying(255),
    "Bio" character varying(1000),
    "ProfilePicture" text,
    "Role" integer NOT NULL,
    "CompanyId" uuid NOT NULL,
    "IsActive" boolean NOT NULL,
    "IsVerified" boolean NOT NULL,
    "EmailVerificationToken" text,
    "EmailVerificationTokenExpiry" timestamp with time zone,
    "PasswordResetToken" text,
    "PasswordResetTokenExpiry" timestamp with time zone,
    "R2RUserId" character varying(255),
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "LastLoginAt" timestamp with time zone,
    "CreatedBy" text,
    "UpdatedBy" text,
    CONSTRAINT "PK_Users" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Users_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE RESTRICT
);

-- Create Collections table
CREATE TABLE "Collections" (
    "Id" uuid NOT NULL,
    "Name" character varying(255) NOT NULL,
    "Description" character varying(1000),
    "Icon" character varying(100),
    "Color" character varying(7),
    "IsPublic" boolean NOT NULL,
    "IsFavorite" boolean NOT NULL,
    "UserId" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "R2RCollectionId" character varying(255),
    "GraphSyncStatus" integer NOT NULL,
    "GraphClusterStatus" integer NOT NULL,
    "LastSyncedAt" timestamp with time zone,
    "TagsJson" text,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CreatedBy" text,
    "UpdatedBy" text,
    CONSTRAINT "PK_Collections" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Collections_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Collections_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);

-- Create Documents table
CREATE TABLE "Documents" (
    "Id" uuid NOT NULL,
    "FileName" character varying(255) NOT NULL,
    "OriginalFileName" character varying(255) NOT NULL,
    "Name" character varying(255) NOT NULL,
    "Description" character varying(1000),
    "FilePath" character varying(500) NOT NULL,
    "ContentType" character varying(100) NOT NULL,
    "SizeInBytes" bigint NOT NULL,
    "FileHash" character varying(64) NOT NULL,
    "DocumentType" integer NOT NULL,
    "Status" integer NOT NULL,
    "StatusMessage" character varying(500),
    "ProcessingProgress" integer NOT NULL,
    "ProcessingError" text,
    "IsProcessing" boolean NOT NULL,
    "IsFavorite" boolean NOT NULL,
    "HasThumbnail" boolean NOT NULL,
    "HasVersions" boolean NOT NULL,
    "Version" integer NOT NULL,
    "ViewCount" integer NOT NULL,
    "LastViewedAt" timestamp with time zone,
    "UserId" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CollectionId" uuid NOT NULL,
    "R2RDocumentId" character varying(255),
    "R2RTaskId" character varying(255),
    "R2RIngestionJobId" character varying(255),
    "R2RProcessedAt" timestamp with time zone,
    "IngestionStatus" integer NOT NULL,
    "ExtractionStatus" integer NOT NULL,
    "Metadata" text,
    "Tags" text,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CreatedBy" text,
    "UpdatedBy" text,
    CONSTRAINT "PK_Documents" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Documents_Collections_CollectionId" FOREIGN KEY ("CollectionId") REFERENCES "Collections" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Documents_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Documents_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);
