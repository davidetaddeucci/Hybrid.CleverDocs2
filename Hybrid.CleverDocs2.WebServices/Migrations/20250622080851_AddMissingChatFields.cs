using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Hybrid.CleverDocs2.WebServices.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingChatFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add missing fields to Messages table (tables already exist)
            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Messages",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

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
                type: "TEXT",
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

            migrationBuilder.AddColumn<string>(
                name: "RagContext",
                table: "Messages",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Messages",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "sent");

            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "Messages",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProcessingTimeMs",
                table: "Messages",
                type: "integer",
                nullable: true);

            // Add missing fields to Conversations table
            migrationBuilder.AddColumn<int>(
                name: "MessageCount",
                table: "Conversations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsPinned",
                table: "Conversations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

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
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "Conversations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "Conversations",
                type: "TEXT",
                nullable: true);

            // Add indexes for new fields
            migrationBuilder.CreateIndex(
                name: "IX_Messages_UserId",
                table: "Messages",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_LastEditedByUserId",
                table: "Messages",
                column: "LastEditedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_IsEdited",
                table: "Messages",
                column: "IsEdited");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_LastEditedAt",
                table: "Messages",
                column: "LastEditedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_Visibility",
                table: "Conversations",
                column: "Visibility");

            // Add foreign key constraints
            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Users_UserId",
                table: "Messages",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Users_LastEditedByUserId",
                table: "Messages",
                column: "LastEditedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop foreign keys
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Users_UserId",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Users_LastEditedByUserId",
                table: "Messages");

            // Drop indexes
            migrationBuilder.DropIndex(
                name: "IX_Messages_UserId",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_LastEditedByUserId",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_IsEdited",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_LastEditedAt",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_Visibility",
                table: "Conversations");

            // Drop columns from Messages
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Messages");

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
                name: "RagContext",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "ProcessingTimeMs",
                table: "Messages");

            // Drop columns from Conversations
            migrationBuilder.DropColumn(
                name: "MessageCount",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "IsPinned",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "Visibility",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "SharedUserIds",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "Conversations");
        }
    }
}
