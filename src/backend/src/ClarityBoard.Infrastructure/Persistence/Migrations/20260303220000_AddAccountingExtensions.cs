using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClarityBoard.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountingExtensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // accounting schema already exists from InitialCreate

            // ── Extend journal_entries ──────────────────────────────────────────

            migrationBuilder.AddColumn<string>(
                name: "DocumentRef",
                schema: "accounting",
                table: "journal_entries",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentRef2",
                schema: "accounting",
                table: "journal_entries",
                type: "character varying(12)",
                maxLength: 12,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DocumentDate",
                schema: "accounting",
                table: "journal_entries",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ServiceDate",
                schema: "accounting",
                table: "journal_entries",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                schema: "accounting",
                table: "journal_entries",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PartnerEntityId",
                schema: "accounting",
                table: "journal_entries",
                type: "uuid",
                nullable: true);

            // ── Extend journal_entry_lines ──────────────────────────────────────

            migrationBuilder.AddColumn<Guid>(
                name: "CostCenterId",
                schema: "accounting",
                table: "journal_entry_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "HrEmployeeId",
                schema: "accounting",
                table: "journal_entry_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "HrTravelExpenseId",
                schema: "accounting",
                table: "journal_entry_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxCode",
                schema: "accounting",
                table: "journal_entry_lines",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            // ── Extend fiscal_periods ───────────────────────────────────────────

            migrationBuilder.AddColumn<int>(
                name: "ExportCount",
                schema: "accounting",
                table: "fiscal_periods",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // ── Extend accounts ─────────────────────────────────────────────────

            migrationBuilder.AddColumn<string>(
                name: "BwaLine",
                schema: "accounting",
                table: "accounts",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAutoPosting",
                schema: "accounting",
                table: "accounts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // ── Create legal_entity_extensions ──────────────────────────────────

            migrationBuilder.CreateTable(
                name: "legal_entity_extensions",
                schema: "accounting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    DatevConsultantNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DatevClientNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    HrbNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    VatId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    TaxOffice = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DefaultCurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true, defaultValue: "EUR"),
                    FiscalYearStartMonth = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true, defaultValue: "01"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_legal_entity_extensions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_legal_entity_extensions_EntityId",
                schema: "accounting",
                table: "legal_entity_extensions",
                column: "EntityId",
                unique: true);

            // ── Create cost_centers ─────────────────────────────────────────────

            migrationBuilder.CreateTable(
                name: "cost_centers",
                schema: "accounting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ShortName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    HrEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    HrDepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cost_centers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cost_centers_EntityId_Code",
                schema: "accounting",
                table: "cost_centers",
                columns: new[] { "EntityId", "Code" },
                unique: true);

            // ── Create accounting_documents ─────────────────────────────────────

            migrationBuilder.CreateTable(
                name: "accounting_documents",
                schema: "accounting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DocumentNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DocumentDate = table.Column<DateOnly>(type: "date", nullable: true),
                    VendorName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TotalAmountCents = table.Column<long>(type: "bigint", nullable: true),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true, defaultValue: "EUR"),
                    StoragePath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    MimeType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "pending"),
                    JournalEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                    AiExtractedData = table.Column<string>(type: "text", nullable: true),
                    AiSuggestedBooking = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UploadedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    RetentionUntil = table.Column<DateOnly>(type: "date", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accounting_documents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_accounting_documents_EntityId_Status",
                schema: "accounting",
                table: "accounting_documents",
                columns: new[] { "EntityId", "Status" });

            // ── Create accounting_scenarios ─────────────────────────────────────

            migrationBuilder.CreateTable(
                name: "accounting_scenarios",
                schema: "accounting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ScenarioType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsBaseline = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accounting_scenarios", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_accounting_scenarios_EntityId_Year",
                schema: "accounting",
                table: "accounting_scenarios",
                columns: new[] { "EntityId", "Year" });

            // ── Create accounting_plan_entries ──────────────────────────────────

            migrationBuilder.CreateTable(
                name: "accounting_plan_entries",
                schema: "accounting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScenarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodYear = table.Column<short>(type: "smallint", nullable: false),
                    PeriodMonth = table.Column<short>(type: "smallint", nullable: false),
                    AmountCents = table.Column<long>(type: "bigint", nullable: false),
                    CostCenterId = table.Column<Guid>(type: "uuid", nullable: true),
                    HrEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accounting_plan_entries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_accounting_plan_entries_ScenarioId_AccountId_PeriodYear_PeriodMonth_HrEmployeeId",
                schema: "accounting",
                table: "accounting_plan_entries",
                columns: new[] { "ScenarioId", "AccountId", "PeriodYear", "PeriodMonth", "HrEmployeeId" });

            // ── Create datev_exports ─────────────────────────────────────────────

            migrationBuilder.CreateTable(
                name: "datev_exports",
                schema: "accounting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    FiscalPeriodId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExportType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    FileCount = table.Column<int>(type: "integer", nullable: true),
                    RecordCount = table.Column<int>(type: "integer", nullable: true),
                    Checksums = table.Column<string>(type: "text", nullable: true),
                    FileStorageKeys = table.Column<string>(type: "text", nullable: true),
                    ErrorDetails = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GeneratedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_datev_exports", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_datev_exports_EntityId_CreatedAt",
                schema: "accounting",
                table: "datev_exports",
                columns: new[] { "EntityId", "CreatedAt" });

            // ── GoBD protection trigger on journal_entries ──────────────────────

            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION accounting.prevent_posted_entry_modification()
                RETURNS TRIGGER AS $$
                BEGIN
                    IF OLD.""Status"" = 'posted' THEN
                        RAISE EXCEPTION 'Cannot modify a posted journal entry. Use reversal instead.';
                    END IF;
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;

                DROP TRIGGER IF EXISTS trg_no_update_posted ON accounting.journal_entries;
                CREATE TRIGGER trg_no_update_posted
                    BEFORE UPDATE ON accounting.journal_entries
                    FOR EACH ROW EXECUTE FUNCTION accounting.prevent_posted_entry_modification();
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_no_update_posted ON accounting.journal_entries;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS accounting.prevent_posted_entry_modification();");

            migrationBuilder.DropTable(name: "datev_exports", schema: "accounting");
            migrationBuilder.DropTable(name: "accounting_plan_entries", schema: "accounting");
            migrationBuilder.DropTable(name: "accounting_scenarios", schema: "accounting");
            migrationBuilder.DropTable(name: "accounting_documents", schema: "accounting");
            migrationBuilder.DropTable(name: "cost_centers", schema: "accounting");
            migrationBuilder.DropTable(name: "legal_entity_extensions", schema: "accounting");

            migrationBuilder.DropColumn(name: "DocumentRef", schema: "accounting", table: "journal_entries");
            migrationBuilder.DropColumn(name: "DocumentRef2", schema: "accounting", table: "journal_entries");
            migrationBuilder.DropColumn(name: "DocumentDate", schema: "accounting", table: "journal_entries");
            migrationBuilder.DropColumn(name: "ServiceDate", schema: "accounting", table: "journal_entries");
            migrationBuilder.DropColumn(name: "Notes", schema: "accounting", table: "journal_entries");
            migrationBuilder.DropColumn(name: "PartnerEntityId", schema: "accounting", table: "journal_entries");

            migrationBuilder.DropColumn(name: "CostCenterId", schema: "accounting", table: "journal_entry_lines");
            migrationBuilder.DropColumn(name: "HrEmployeeId", schema: "accounting", table: "journal_entry_lines");
            migrationBuilder.DropColumn(name: "HrTravelExpenseId", schema: "accounting", table: "journal_entry_lines");
            migrationBuilder.DropColumn(name: "TaxCode", schema: "accounting", table: "journal_entry_lines");

            migrationBuilder.DropColumn(name: "ExportCount", schema: "accounting", table: "fiscal_periods");
            migrationBuilder.DropColumn(name: "BwaLine", schema: "accounting", table: "accounts");
            migrationBuilder.DropColumn(name: "IsAutoPosting", schema: "accounting", table: "accounts");
        }
    }
}
