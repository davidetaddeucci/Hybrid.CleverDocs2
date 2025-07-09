using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hybrid.CleverDocs2.WebServices.Migrations
{
    /// <inheritdoc />
    public partial class AddUserLLMPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Version",
                table: "Documents",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

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

            migrationBuilder.AlterColumn<string>(
                name: "FileHash",
                table: "Documents",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "UserLLMPreferences",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ApiEndpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EncryptedApiKey = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Temperature = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: false),
                    MaxTokens = table.Column<int>(type: "integer", nullable: false),
                    TopP = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: true),
                    EnableStreaming = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    AdditionalParameters = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UsageCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLLMPreferences", x => x.UserId);
                    table.CheckConstraint("CK_UserLLMPreferences_MaxTokens", "\"MaxTokens\" >= 1 AND \"MaxTokens\" <= 32000");
                    table.CheckConstraint("CK_UserLLMPreferences_Provider", "\"Provider\" IN ('openai', 'anthropic', 'azure', 'custom')");
                    table.CheckConstraint("CK_UserLLMPreferences_Temperature", "\"Temperature\" >= 0.0 AND \"Temperature\" <= 2.0");
                    table.CheckConstraint("CK_UserLLMPreferences_TopP", "\"TopP\" IS NULL OR (\"TopP\" >= 0.0 AND \"TopP\" <= 1.0)");
                    table.ForeignKey(
                        name: "FK_UserLLMPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserLLMPreferences_IsActive",
                table: "UserLLMPreferences",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_UserLLMPreferences_LastUsedAt",
                table: "UserLLMPreferences",
                column: "LastUsedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserLLMPreferences_Provider",
                table: "UserLLMPreferences",
                column: "Provider");

            migrationBuilder.CreateIndex(
                name: "IX_UserLLMPreferences_Provider_IsActive",
                table: "UserLLMPreferences",
                columns: new[] { "Provider", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserLLMPreferences");

            migrationBuilder.AlterColumn<string>(
                name: "Version",
                table: "Documents",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

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

            migrationBuilder.AlterColumn<string>(
                name: "FileHash",
                table: "Documents",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);
        }
    }
}
