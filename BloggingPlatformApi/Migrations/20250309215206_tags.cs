using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BloggingPlatformApi.Migrations
{
    /// <inheritdoc />
    public partial class tags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TagId1",
                table: "PostTags",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostTags_TagId1",
                table: "PostTags",
                column: "TagId1");

            migrationBuilder.AddForeignKey(
                name: "FK_PostTags_Tags_TagId1",
                table: "PostTags",
                column: "TagId1",
                principalTable: "Tags",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PostTags_Tags_TagId1",
                table: "PostTags");

            migrationBuilder.DropIndex(
                name: "IX_PostTags_TagId1",
                table: "PostTags");

            migrationBuilder.DropColumn(
                name: "TagId1",
                table: "PostTags");
        }
    }
}
