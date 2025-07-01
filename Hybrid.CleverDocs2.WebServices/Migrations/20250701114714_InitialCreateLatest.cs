using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hybrid.CleverDocs2.WebServices.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateLatest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Settings",
                table: "Conversations",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CollectionIds",
                table: "Conversations",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Settings",
                table: "Conversations",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CollectionIds",
                table: "Conversations",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");
        }
    }
}
