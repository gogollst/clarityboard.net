using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClarityBoard.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEntityFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EntityId",
                schema: "integration",
                table: "webhook_events",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MappingRuleId",
                schema: "integration",
                table: "webhook_events",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProcessingDurationMs",
                schema: "integration",
                table: "webhook_events",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecoveryCodesHash",
                schema: "public",
                table: "users",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "BaselineDate",
                schema: "scenario",
                table: "scenarios",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ComparedToScenarioId",
                schema: "scenario",
                table: "scenarios",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DatevClientNumber",
                schema: "entity",
                table: "legal_entities",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DatevConsultantNumber",
                schema: "entity",
                table: "legal_entities",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManagingDirector",
                schema: "entity",
                table: "legal_entities",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Direction",
                schema: "kpi",
                table: "kpi_definitions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AddColumn<string>(
                name: "CalculationClass",
                schema: "kpi",
                table: "kpi_definitions",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                schema: "kpi",
                table: "kpi_definitions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AfaCode",
                schema: "asset",
                table: "fixed_assets",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                schema: "asset",
                table: "fixed_assets",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SerialNumber",
                schema: "asset",
                table: "fixed_assets",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExportedAt",
                schema: "accounting",
                table: "fiscal_periods",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ExportedBy",
                schema: "accounting",
                table: "fiscal_periods",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                schema: "document",
                table: "documents",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "InvoiceDate",
                schema: "document",
                table: "documents",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceNumber",
                schema: "document",
                table: "documents",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAmount",
                schema: "document",
                table: "documents",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VendorName",
                schema: "document",
                table: "documents",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Certainty",
                schema: "cashflow",
                table: "cash_flow_entries",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "confirmed");

            migrationBuilder.AddColumn<int>(
                name: "PaymentTermsDays",
                schema: "cashflow",
                table: "cash_flow_entries",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Department",
                schema: "budget",
                table: "budgets",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "budget",
                table: "budgets",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CostCenterDefault",
                schema: "accounting",
                table: "accounts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSystemAccount",
                schema: "accounting",
                table: "accounts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_webhook_events_EntityId_Status",
                schema: "integration",
                table: "webhook_events",
                columns: new[] { "EntityId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_webhook_events_EntityId_Status",
                schema: "integration",
                table: "webhook_events");

            migrationBuilder.DropColumn(
                name: "EntityId",
                schema: "integration",
                table: "webhook_events");

            migrationBuilder.DropColumn(
                name: "MappingRuleId",
                schema: "integration",
                table: "webhook_events");

            migrationBuilder.DropColumn(
                name: "ProcessingDurationMs",
                schema: "integration",
                table: "webhook_events");

            migrationBuilder.DropColumn(
                name: "RecoveryCodesHash",
                schema: "public",
                table: "users");

            migrationBuilder.DropColumn(
                name: "BaselineDate",
                schema: "scenario",
                table: "scenarios");

            migrationBuilder.DropColumn(
                name: "ComparedToScenarioId",
                schema: "scenario",
                table: "scenarios");

            migrationBuilder.DropColumn(
                name: "DatevClientNumber",
                schema: "entity",
                table: "legal_entities");

            migrationBuilder.DropColumn(
                name: "DatevConsultantNumber",
                schema: "entity",
                table: "legal_entities");

            migrationBuilder.DropColumn(
                name: "ManagingDirector",
                schema: "entity",
                table: "legal_entities");

            migrationBuilder.DropColumn(
                name: "CalculationClass",
                schema: "kpi",
                table: "kpi_definitions");

            migrationBuilder.DropColumn(
                name: "Category",
                schema: "kpi",
                table: "kpi_definitions");

            migrationBuilder.DropColumn(
                name: "AfaCode",
                schema: "asset",
                table: "fixed_assets");

            migrationBuilder.DropColumn(
                name: "Location",
                schema: "asset",
                table: "fixed_assets");

            migrationBuilder.DropColumn(
                name: "SerialNumber",
                schema: "asset",
                table: "fixed_assets");

            migrationBuilder.DropColumn(
                name: "ExportedAt",
                schema: "accounting",
                table: "fiscal_periods");

            migrationBuilder.DropColumn(
                name: "ExportedBy",
                schema: "accounting",
                table: "fiscal_periods");

            migrationBuilder.DropColumn(
                name: "Currency",
                schema: "document",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "InvoiceDate",
                schema: "document",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "InvoiceNumber",
                schema: "document",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "TotalAmount",
                schema: "document",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "VendorName",
                schema: "document",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "Certainty",
                schema: "cashflow",
                table: "cash_flow_entries");

            migrationBuilder.DropColumn(
                name: "PaymentTermsDays",
                schema: "cashflow",
                table: "cash_flow_entries");

            migrationBuilder.DropColumn(
                name: "Department",
                schema: "budget",
                table: "budgets");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "budget",
                table: "budgets");

            migrationBuilder.DropColumn(
                name: "CostCenterDefault",
                schema: "accounting",
                table: "accounts");

            migrationBuilder.DropColumn(
                name: "IsSystemAccount",
                schema: "accounting",
                table: "accounts");

            migrationBuilder.AlterColumn<string>(
                name: "Direction",
                schema: "kpi",
                table: "kpi_definitions",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);
        }
    }
}
