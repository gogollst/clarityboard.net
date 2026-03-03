using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClarityBoard.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserInvitationAndStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Make password_hash nullable (invited users have no password yet)
            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                schema: "public",
                table: "users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            // Add invitation token columns
            migrationBuilder.AddColumn<string>(
                name: "InvitationToken",
                schema: "public",
                table: "users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InvitationTokenExpiry",
                schema: "public",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            // Add status column (0=Active, 1=Invited, 2=Inactive)
            migrationBuilder.AddColumn<int>(
                name: "Status",
                schema: "public",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Backfill status from is_active
            migrationBuilder.Sql("UPDATE public.users SET \"Status\" = 0 WHERE \"IsActive\" = true");
            migrationBuilder.Sql("UPDATE public.users SET \"Status\" = 2 WHERE \"IsActive\" = false");

            // Unique index on InvitationToken (only for non-null values)
            migrationBuilder.CreateIndex(
                name: "IX_users_InvitationToken",
                schema: "public",
                table: "users",
                column: "InvitationToken",
                unique: true,
                filter: "\"InvitationToken\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_InvitationToken",
                schema: "public",
                table: "users");

            migrationBuilder.DropColumn(
                name: "InvitationToken",
                schema: "public",
                table: "users");

            migrationBuilder.DropColumn(
                name: "InvitationTokenExpiry",
                schema: "public",
                table: "users");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "public",
                table: "users");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                schema: "public",
                table: "users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);
        }
    }
}
