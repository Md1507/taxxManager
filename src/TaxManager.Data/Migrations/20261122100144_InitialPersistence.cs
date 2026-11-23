using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaxManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Dependents",
                columns: table => new
                {
                    ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dependents", x => new { x.ProfileId, x.Id });
                    table.ForeignKey(
                        name: "FK_Dependents_UserProfiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MonthlyIncomes",
                columns: table => new
                {
                    ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    Month = table.Column<int>(type: "INTEGER", nullable: false),
                    GrossIncome = table.Column<decimal>(type: "TEXT", nullable: false),
                    InsuranceAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    ActualWithheldTax = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyIncomes", x => new { x.ProfileId, x.Year, x.Month });
                    table.CheckConstraint("CK_Income_Month", "Month BETWEEN 1 AND 12");
                    table.CheckConstraint("CK_Income_Year", "Year BETWEEN 1 AND 9999");
                    table.ForeignKey(
                        name: "FK_MonthlyIncomes_UserProfiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DependentEligibilityPeriods",
                columns: table => new
                {
                    ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DependentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EligibleFromMonth = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EligibleToMonth = table.Column<DateOnly>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DependentEligibilityPeriods", x => new { x.ProfileId, x.DependentId, x.EligibleFromMonth });
                    table.CheckConstraint("CK_Period_Dates", "length(EligibleFromMonth) = 10 AND length(EligibleToMonth) = 10 AND EligibleFromMonth GLOB '[0-9][0-9][0-9][0-9]-[0-9][0-9]-01' AND EligibleToMonth GLOB '[0-9][0-9][0-9][0-9]-[0-9][0-9]-01' AND substr(EligibleFromMonth,1,4) BETWEEN '0001' AND '9999' AND substr(EligibleToMonth,1,4) BETWEEN '0001' AND '9999' AND substr(EligibleFromMonth,6,2) BETWEEN '01' AND '12' AND substr(EligibleToMonth,6,2) BETWEEN '01' AND '12' AND EligibleFromMonth <= EligibleToMonth");
                    table.ForeignKey(
                        name: "FK_DependentEligibilityPeriods_Dependents_ProfileId_DependentId",
                        columns: x => new { x.ProfileId, x.DependentId },
                        principalTable: "Dependents",
                        principalColumns: new[] { "ProfileId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });
            migrationBuilder.Sql("""
                CREATE TRIGGER Period_NoOverlap_Insert BEFORE INSERT ON DependentEligibilityPeriods
                WHEN EXISTS (SELECT 1 FROM DependentEligibilityPeriods p WHERE p.ProfileId = NEW.ProfileId AND p.DependentId = NEW.DependentId AND NEW.EligibleFromMonth <= p.EligibleToMonth AND NEW.EligibleToMonth >= p.EligibleFromMonth)
                BEGIN SELECT RAISE(ABORT, 'Overlapping dependent periods'); END;
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER Period_NoOverlap_Update BEFORE UPDATE ON DependentEligibilityPeriods
                WHEN EXISTS (SELECT 1 FROM DependentEligibilityPeriods p WHERE p.ProfileId = NEW.ProfileId AND p.DependentId = NEW.DependentId AND NEW.EligibleFromMonth <= p.EligibleToMonth AND NEW.EligibleToMonth >= p.EligibleFromMonth AND NOT (p.ProfileId = OLD.ProfileId AND p.DependentId = OLD.DependentId AND p.EligibleFromMonth = OLD.EligibleFromMonth))
                BEGIN SELECT RAISE(ABORT, 'Overlapping dependent periods'); END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Period_NoOverlap_Insert;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Period_NoOverlap_Update;");
            migrationBuilder.DropTable(
                name: "DependentEligibilityPeriods");

            migrationBuilder.DropTable(
                name: "MonthlyIncomes");

            migrationBuilder.DropTable(
                name: "Dependents");

            migrationBuilder.DropTable(
                name: "UserProfiles");
        }
    }
}
