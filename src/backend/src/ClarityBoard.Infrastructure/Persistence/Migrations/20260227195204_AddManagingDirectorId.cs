using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClarityBoard.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddManagingDirectorId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ManagingDirectorId",
                schema: "entity",
                table: "legal_entities",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_legal_entities_ManagingDirectorId",
                schema: "entity",
                table: "legal_entities",
                column: "ManagingDirectorId");

            migrationBuilder.AddForeignKey(
                name: "FK_legal_entities_users_ManagingDirectorId",
                schema: "entity",
                table: "legal_entities",
                column: "ManagingDirectorId",
                principalSchema: "public",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_legal_entities_users_ManagingDirectorId",
                schema: "entity",
                table: "legal_entities");

            migrationBuilder.DropIndex(
                name: "IX_legal_entities_ManagingDirectorId",
                schema: "entity",
                table: "legal_entities");

            migrationBuilder.DropColumn(
                name: "ManagingDirectorId",
                schema: "entity",
                table: "legal_entities");
        }
    }
}
