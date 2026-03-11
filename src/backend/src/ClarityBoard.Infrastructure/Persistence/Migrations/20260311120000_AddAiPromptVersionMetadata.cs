using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClarityBoard.Infrastructure.Persistence.Migrations;

public partial class AddAiPromptVersionMetadata : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "FallbackModel",
            schema: "ai",
            table: "ai_prompt_versions",
            type: "character varying(100)",
            maxLength: 100,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<int>(
            name: "MaxTokens",
            schema: "ai",
            table: "ai_prompt_versions",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<string>(
            name: "PrimaryModel",
            schema: "ai",
            table: "ai_prompt_versions",
            type: "character varying(100)",
            maxLength: 100,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<decimal>(
            name: "Temperature",
            schema: "ai",
            table: "ai_prompt_versions",
            type: "numeric(4,2)",
            precision: 4,
            scale: 2,
            nullable: false,
            defaultValue: 0m);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "FallbackModel", schema: "ai", table: "ai_prompt_versions");
        migrationBuilder.DropColumn(name: "MaxTokens", schema: "ai", table: "ai_prompt_versions");
        migrationBuilder.DropColumn(name: "PrimaryModel", schema: "ai", table: "ai_prompt_versions");
        migrationBuilder.DropColumn(name: "Temperature", schema: "ai", table: "ai_prompt_versions");
    }
}