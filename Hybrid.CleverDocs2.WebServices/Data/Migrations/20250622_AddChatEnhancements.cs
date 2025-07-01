using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hybrid.CleverDocs2.WebServices.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChatEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add message editing and history fields
            migrationBuilder.AddColumn<bool>(
                name: "IsEdited",
                table: "Messages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OriginalContent",
                table: "Messages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EditHistory",
                table: "Messages",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastEditedAt",
                table: "Messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastEditedByUserId",
                table: "Messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Messages",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Add conversation sharing and organization fields
            migrationBuilder.AddColumn<string>(
                name: "Visibility",
                table: "Conversations",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "private");

            migrationBuilder.AddColumn<string>(
                name: "SharedUserIds",
                table: "Conversations",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "Conversations",
                type: "jsonb",
                nullable: true);

            // Add foreign key constraint for LastEditedByUserId
            migrationBuilder.CreateIndex(
                name: "IX_Messages_LastEditedByUserId",
                table: "Messages",
                column: "LastEditedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Users_LastEditedByUserId",
                table: "Messages",
                column: "LastEditedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Users_UserId",
                table: "Messages",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Add indexes for performance
            migrationBuilder.CreateIndex(
                name: "IX_Conversations_Visibility",
                table: "Conversations",
                column: "Visibility");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_IsEdited",
                table: "Messages",
                column: "IsEdited");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_LastEditedAt",
                table: "Messages",
                column: "LastEditedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop foreign key and indexes
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Users_LastEditedByUserId",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Users_UserId",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_LastEditedByUserId",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_Visibility",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_Messages_IsEdited",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_LastEditedAt",
                table: "Messages");

            // Drop message editing fields
            migrationBuilder.DropColumn(
                name: "IsEdited",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "OriginalContent",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "EditHistory",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "LastEditedAt",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "LastEditedByUserId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Messages");

            // Drop conversation sharing fields
            migrationBuilder.DropColumn(
                name: "Visibility",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "SharedUserIds",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Conversations");
        }
    }
}
