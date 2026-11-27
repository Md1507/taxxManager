using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaxManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAnnualForecastResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnnualCalculationHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    InputRevision = table.Column<int>(type: "INTEGER", nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    SchemaVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    PayloadEncrypted = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnnualCalculationHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AnnualForecastRules",
                columns: table => new
                {
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    PeriodFrom = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    PeriodTo = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Graph = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnnualForecastRules", x => new { x.Year, x.PeriodFrom });
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnnualCalculationHistory_DocumentId_CreatedAtUtc",
                table: "AnnualCalculationHistory",
                columns: new[] { "DocumentId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Never drop persisted annual evidence during an ordinary downgrade.
            migrationBuilder.Sql("""
                CREATE TABLE __AnnualHistoryDownGuard (Value INTEGER CHECK (Value = 0));
                INSERT INTO __AnnualHistoryDownGuard SELECT count(*) FROM AnnualCalculationHistory;
                DROP TABLE __AnnualHistoryDownGuard;
                """);
            migrationBuilder.DropTable(
                name: "AnnualCalculationHistory");

            migrationBuilder.DropTable(
                name: "AnnualForecastRules");
        }
    }
}
