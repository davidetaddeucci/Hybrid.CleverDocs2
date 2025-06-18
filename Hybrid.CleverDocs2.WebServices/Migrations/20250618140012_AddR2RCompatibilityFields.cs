using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hybrid.CleverDocs2.WebServices.Migrations
{
    /// <inheritdoc />
    public partial class AddR2RCompatibilityFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Size",
                table: "Documents");

            migrationBuilder.RenameColumn(
                name: "IsEmailVerified",
                table: "Users",
                newName: "IsVerified");

            migrationBuilder.RenameColumn(
                name: "SizeBytes",
                table: "Documents",
                newName: "SizeInBytes");

            migrationBuilder.AddColumn<string>(
                name: "Bio",
                table: "Users",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfilePicture",
                table: "Users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Tags",
                table: "Documents",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(List<string>),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "Metadata",
                table: "Documents",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Dictionary<string, object>),
                oldType: "jsonb");

            migrationBuilder.AddColumn<string>(
                name: "DocumentType",
                table: "Documents",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExtractionStatus",
                table: "Documents",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IngestionStatus",
                table: "Documents",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Companies",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "GraphClusterStatus",
                table: "Collections",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GraphSyncStatus",
                table: "Collections",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncedAt",
                table: "Collections",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Bio",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ProfilePicture",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DocumentType",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ExtractionStatus",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "IngestionStatus",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "GraphClusterStatus",
                table: "Collections");

            migrationBuilder.DropColumn(
                name: "GraphSyncStatus",
                table: "Collections");

            migrationBuilder.DropColumn(
                name: "LastSyncedAt",
                table: "Collections");

            migrationBuilder.RenameColumn(
                name: "IsVerified",
                table: "Users",
                newName: "IsEmailVerified");

            migrationBuilder.RenameColumn(
                name: "SizeInBytes",
                table: "Documents",
                newName: "SizeBytes");

            migrationBuilder.AlterColumn<List<string>>(
                name: "Tags",
                table: "Documents",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Dictionary<string, object>>(
                name: "Metadata",
                table: "Documents",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<long>(
                name: "Size",
                table: "Documents",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }
    }
}
