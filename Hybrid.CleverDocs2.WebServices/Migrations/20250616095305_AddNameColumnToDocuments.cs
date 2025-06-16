using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hybrid.CleverDocs2.WebServices.Migrations
{
    /// <inheritdoc />
    public partial class AddNameColumnToDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CollectionId",
                table: "Documents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasThumbnail",
                table: "Documents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasVersions",
                table: "Documents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsFavorite",
                table: "Documents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsProcessing",
                table: "Documents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastViewedAt",
                table: "Documents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Dictionary<string, object>>(
                name: "Metadata",
                table: "Documents",
                type: "jsonb",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Documents",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProcessingError",
                table: "Documents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ProcessingProgress",
                table: "Documents",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Size",
                table: "Documents",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<List<string>>(
                name: "Tags",
                table: "Documents",
                type: "jsonb",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "Version",
                table: "Documents",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ViewCount",
                table: "Documents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_CollectionId",
                table: "Documents",
                column: "CollectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Collections_CollectionId",
                table: "Documents",
                column: "CollectionId",
                principalTable: "Collections",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Collections_CollectionId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_CollectionId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "CollectionId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "HasThumbnail",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "HasVersions",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "IsFavorite",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "IsProcessing",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "LastViewedAt",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ProcessingError",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ProcessingProgress",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "Size",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ViewCount",
                table: "Documents");
        }
    }
}
