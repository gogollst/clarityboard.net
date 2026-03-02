using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClarityBoard.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMailInfrastructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(name: "mail");

            migrationBuilder.CreateTable(
                name: "mail_configs",
                schema: "mail",
                columns: table => new
                {
                    Id                = table.Column<Guid>(type: "uuid", nullable: false),
                    Host              = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Port              = table.Column<int>(type: "integer", nullable: false),
                    Username          = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EncryptedPassword = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    FromEmail         = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FromName          = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    EnableSsl         = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive          = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt         = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt         = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mail_configs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "email_logs",
                schema: "mail",
                columns: table => new
                {
                    Id           = table.Column<Guid>(type: "uuid", nullable: false),
                    Type         = table.Column<int>(type: "integer", nullable: false),
                    ToEmail      = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Subject      = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status       = table.Column<int>(type: "integer", nullable: false),
                    Attempts     = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    UserId       = table.Column<Guid>(type: "uuid", nullable: true),
                    SentAt       = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_logs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_email_logs_SentAt",
                schema: "mail",
                table: "email_logs",
                column: "SentAt");

            migrationBuilder.CreateIndex(
                name: "IX_email_logs_UserId",
                schema: "mail",
                table: "email_logs",
                column: "UserId");

            // Add password-reset token columns to existing users table (public schema)
            migrationBuilder.AddColumn<string>(
                name: "PasswordResetToken",
                schema: "public",
                table: "users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetTokenExpiry",
                schema: "public",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "email_logs", schema: "mail");
            migrationBuilder.DropTable(name: "mail_configs", schema: "mail");
            migrationBuilder.DropColumn(name: "PasswordResetToken", schema: "public", table: "users");
            migrationBuilder.DropColumn(name: "PasswordResetTokenExpiry", schema: "public", table: "users");
        }
    }
}
