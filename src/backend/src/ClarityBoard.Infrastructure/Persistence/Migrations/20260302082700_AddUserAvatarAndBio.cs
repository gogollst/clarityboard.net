using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClarityBoard.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAvatarAndBio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AvatarPath",
                schema: "public",
                table: "users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Bio",
                schema: "public",
                table: "users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvatarPath",
                schema: "public",
                table: "users");

            migrationBuilder.DropColumn(
                name: "Bio",
                schema: "public",
                table: "users");
        }
    }
}
