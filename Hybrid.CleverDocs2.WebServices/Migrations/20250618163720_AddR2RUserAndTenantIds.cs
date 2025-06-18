using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hybrid.CleverDocs2.WebServices.Migrations
{
    /// <inheritdoc />
    public partial class AddR2RUserAndTenantIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "R2RUserId",
                table: "Users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "R2RTenantId",
                table: "Companies",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "R2RUserId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "R2RTenantId",
                table: "Companies");
        }
    }
}
