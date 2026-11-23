using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaxManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCalculationHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CalculationHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    Month = table.Column<int>(type: "INTEGER", nullable: false),
                    SchemaVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    PayloadEncrypted = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalculationHistory", x => x.Id);
                    table.CheckConstraint("CK_History_Month", "Month BETWEEN 1 AND 12");
                    table.CheckConstraint("CK_History_Schema", "SchemaVersion = 1");
                    table.CheckConstraint("CK_History_Year", "Year BETWEEN 1 AND 9999");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CalculationHistory_ProfileId_Year_Month_CreatedAtUtc",
                table: "CalculationHistory",
                columns: new[] { "ProfileId", "Year", "Month", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("Calculation history downgrade requires restoring a matching backup; dropping immutable history is not supported.");
        }
    }
}
