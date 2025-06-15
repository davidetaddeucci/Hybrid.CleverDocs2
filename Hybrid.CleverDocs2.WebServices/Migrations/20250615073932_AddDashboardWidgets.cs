using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hybrid.CleverDocs2.WebServices.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardWidgets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserDashboardWidgets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    WidgetType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    WidgetId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Configuration = table.Column<string>(type: "jsonb", nullable: false),
                    PositionX = table.Column<int>(type: "integer", nullable: false),
                    PositionY = table.Column<int>(type: "integer", nullable: false),
                    Width = table.Column<int>(type: "integer", nullable: false),
                    Height = table.Column<int>(type: "integer", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsVisible = table.Column<bool>(type: "boolean", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    MinimumRole = table.Column<int>(type: "integer", nullable: false),
                    RefreshInterval = table.Column<int>(type: "integer", nullable: false),
                    Theme = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CssClasses = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DataSource = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CacheTtl = table.Column<int>(type: "integer", nullable: false),
                    SupportsExport = table.Column<bool>(type: "boolean", nullable: false),
                    SupportsClick = table.Column<bool>(type: "boolean", nullable: false),
                    ClickAction = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDashboardWidgets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserDashboardWidgets_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserDashboardWidgets_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WidgetTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    WidgetType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DefaultConfiguration = table.Column<string>(type: "jsonb", nullable: false),
                    DefaultWidth = table.Column<int>(type: "integer", nullable: false),
                    DefaultHeight = table.Column<int>(type: "integer", nullable: false),
                    MinimumRole = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Icon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WidgetTemplates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserDashboardWidgets_CompanyId",
                table: "UserDashboardWidgets",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDashboardWidgets_UserId",
                table: "UserDashboardWidgets",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserDashboardWidgets");

            migrationBuilder.DropTable(
                name: "WidgetTemplates");
        }
    }
}
