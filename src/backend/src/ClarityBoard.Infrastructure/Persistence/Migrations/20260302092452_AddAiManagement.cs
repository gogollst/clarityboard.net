using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClarityBoard.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAiManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ai");

            migrationBuilder.CreateTable(
                name: "ai_prompts",
                schema: "ai",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PromptKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Module = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FunctionDescription = table.Column<string>(type: "text", nullable: false),
                    SystemPrompt = table.Column<string>(type: "text", nullable: false),
                    UserPromptTemplate = table.Column<string>(type: "text", nullable: true),
                    ExampleInput = table.Column<string>(type: "text", nullable: true),
                    ExampleOutput = table.Column<string>(type: "text", nullable: true),
                    PrimaryProvider = table.Column<int>(type: "integer", nullable: false),
                    PrimaryModel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FallbackProvider = table.Column<int>(type: "integer", nullable: false),
                    FallbackModel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Temperature = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false),
                    MaxTokens = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsSystemPrompt = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastEditedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_prompts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ai_provider_configs",
                schema: "ai",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<int>(type: "integer", nullable: false),
                    EncryptedApiKey = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    KeyHint = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsHealthy = table.Column<bool>(type: "boolean", nullable: false),
                    LastTestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    BaseUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ModelDefault = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_provider_configs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ai_call_logs",
                schema: "ai",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PromptId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsedProvider = table.Column<int>(type: "integer", nullable: false),
                    UsedFallback = table.Column<bool>(type: "boolean", nullable: false),
                    InputTokens = table.Column<int>(type: "integer", nullable: false),
                    OutputTokens = table.Column<int>(type: "integer", nullable: false),
                    DurationMs = table.Column<int>(type: "integer", nullable: false),
                    IsSuccess = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_call_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ai_call_logs_ai_prompts_PromptId",
                        column: x => x.PromptId,
                        principalSchema: "ai",
                        principalTable: "ai_prompts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ai_prompt_versions",
                schema: "ai",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PromptId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    SystemPrompt = table.Column<string>(type: "text", nullable: false),
                    UserPromptTemplate = table.Column<string>(type: "text", nullable: true),
                    PrimaryProvider = table.Column<int>(type: "integer", nullable: false),
                    FallbackProvider = table.Column<int>(type: "integer", nullable: false),
                    ChangeSummary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_prompt_versions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ai_prompt_versions_ai_prompts_PromptId",
                        column: x => x.PromptId,
                        principalSchema: "ai",
                        principalTable: "ai_prompts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ai_call_logs_IsSuccess_CreatedAt",
                schema: "ai",
                table: "ai_call_logs",
                columns: new[] { "IsSuccess", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ai_call_logs_PromptId_CreatedAt",
                schema: "ai",
                table: "ai_call_logs",
                columns: new[] { "PromptId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ai_prompt_versions_PromptId_Version",
                schema: "ai",
                table: "ai_prompt_versions",
                columns: new[] { "PromptId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ai_prompts_PromptKey",
                schema: "ai",
                table: "ai_prompts",
                column: "PromptKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ai_provider_configs_Provider_IsActive",
                schema: "ai",
                table: "ai_provider_configs",
                columns: new[] { "Provider", "IsActive" },
                unique: true,
                filter: "\"IsActive\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_call_logs",
                schema: "ai");

            migrationBuilder.DropTable(
                name: "ai_prompt_versions",
                schema: "ai");

            migrationBuilder.DropTable(
                name: "ai_provider_configs",
                schema: "ai");

            migrationBuilder.DropTable(
                name: "ai_prompts",
                schema: "ai");
        }
    }
}
