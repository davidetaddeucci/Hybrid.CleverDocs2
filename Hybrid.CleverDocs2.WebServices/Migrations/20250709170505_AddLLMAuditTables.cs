using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hybrid.CleverDocs2.WebServices.Migrations
{
    /// <inheritdoc />
    public partial class AddLLMAuditTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LLMAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    OldConfiguration = table.Column<string>(type: "text", nullable: true),
                    NewConfiguration = table.Column<string>(type: "text", nullable: true),
                    ChangedBy = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LLMAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LLMAuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LLMUsageLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    Model = table.Column<string>(type: "text", nullable: false),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ResponseTimeMs = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LLMUsageLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LLMUsageLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LLMAuditLogs_Action",
                table: "LLMAuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_LLMAuditLogs_Timestamp",
                table: "LLMAuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_LLMAuditLogs_UserId",
                table: "LLMAuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LLMAuditLogs_UserId_Timestamp",
                table: "LLMAuditLogs",
                columns: new[] { "UserId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_LLMUsageLogs_Provider",
                table: "LLMUsageLogs",
                column: "Provider");

            migrationBuilder.CreateIndex(
                name: "IX_LLMUsageLogs_Success",
                table: "LLMUsageLogs",
                column: "Success");

            migrationBuilder.CreateIndex(
                name: "IX_LLMUsageLogs_Timestamp",
                table: "LLMUsageLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_LLMUsageLogs_UserId",
                table: "LLMUsageLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LLMUsageLogs_UserId_Provider_Timestamp",
                table: "LLMUsageLogs",
                columns: new[] { "UserId", "Provider", "Timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LLMAuditLogs");

            migrationBuilder.DropTable(
                name: "LLMUsageLogs");
        }
    }
}
