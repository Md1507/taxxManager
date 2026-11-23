using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaxManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMonthlyTaxRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaxRuleLegalBasis",
                columns: table => new
                {
                    Key = table.Column<Guid>(type: "TEXT", nullable: false),
                    Id = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<string>(type: "TEXT", nullable: true),
                    Point = table.Column<string>(type: "TEXT", nullable: true),
                    Clause = table.Column<string>(type: "TEXT", nullable: true),
                    Article = table.Column<string>(type: "TEXT", nullable: true),
                    DocumentNumber = table.Column<string>(type: "TEXT", nullable: true),
                    Summary = table.Column<string>(type: "TEXT", nullable: true),
                    SourceUrl = table.Column<string>(type: "TEXT", nullable: true),
                    Period = table.Column<string>(type: "TEXT", nullable: true),
                    EffectiveFrom = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    EffectiveTo = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    VerifiedOn = table.Column<DateOnly>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxRuleLegalBasis", x => x.Key);
                    table.CheckConstraint("CK_TaxRuleLegalBasis_Metadata",
                        "typeof(Status) = 'integer' AND " +
                        "Status BETWEEN 0 AND 2 AND " +
                        "(EffectiveFrom IS NULL OR (length(EffectiveFrom) = 10 AND " +
                        "strftime('%Y-%m-%d', EffectiveFrom, '+0 days') IS EffectiveFrom AND " +
                        "substr(EffectiveFrom, 1, 4) BETWEEN '0001' AND '9999')) AND " +
                        "(EffectiveTo IS NULL OR (length(EffectiveTo) = 10 AND " +
                        "strftime('%Y-%m-%d', EffectiveTo, '+0 days') IS EffectiveTo AND " +
                        "substr(EffectiveTo, 1, 4) BETWEEN '0001' AND '9999')) AND " +
                        "(VerifiedOn IS NULL OR (length(VerifiedOn) = 10 AND " +
                        "strftime('%Y-%m-%d', VerifiedOn, '+0 days') IS VerifiedOn AND " +
                        "substr(VerifiedOn, 1, 4) BETWEEN '0001' AND '9999')) AND " +
                        "(EffectiveFrom IS NULL OR EffectiveTo IS NULL OR EffectiveFrom <= EffectiveTo) AND " +
                        "(Status = 2 OR VerifiedOn IS NULL)");
                });

            migrationBuilder.CreateTable(
                name: "TaxRuleSnapshot",
                columns: table => new
                {
                    RuleId = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<string>(type: "TEXT", nullable: false),
                    Period = table.Column<string>(type: "TEXT", nullable: false),
                    FromYear = table.Column<int>(type: "INTEGER", nullable: false),
                    PersonalDeduction = table.Column<decimal>(type: "TEXT", nullable: false),
                    DependentDeduction = table.Column<decimal>(type: "TEXT", nullable: false),
                    PersonalBasisKey = table.Column<Guid>(type: "TEXT", nullable: false),
                    DependentBasisKey = table.Column<Guid>(type: "TEXT", nullable: false),
                    IncomeBasisKey = table.Column<Guid>(type: "TEXT", nullable: false),
                    TaxBasisKey = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxRuleSnapshot", x => new { x.RuleId, x.Version });
                    table.CheckConstraint("CK_TaxRuleSnapshot_Year",
                        "typeof(FromYear) = 'integer' AND FromYear BETWEEN 1 AND 9999");
                    table.ForeignKey(
                        name: "FK_TaxRuleSnapshot_TaxRuleLegalBasis_DependentBasisKey",
                        column: x => x.DependentBasisKey,
                        principalTable: "TaxRuleLegalBasis",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaxRuleSnapshot_TaxRuleLegalBasis_IncomeBasisKey",
                        column: x => x.IncomeBasisKey,
                        principalTable: "TaxRuleLegalBasis",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaxRuleSnapshot_TaxRuleLegalBasis_PersonalBasisKey",
                        column: x => x.PersonalBasisKey,
                        principalTable: "TaxRuleLegalBasis",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaxRuleSnapshot_TaxRuleLegalBasis_TaxBasisKey",
                        column: x => x.TaxBasisKey,
                        principalTable: "TaxRuleLegalBasis",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaxRuleBracket",
                columns: table => new
                {
                    RuleId = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<string>(type: "TEXT", nullable: false),
                    Ordinal = table.Column<int>(type: "INTEGER", nullable: false),
                    UpperBound = table.Column<decimal>(type: "TEXT", nullable: true),
                    Rate = table.Column<decimal>(type: "TEXT", nullable: false),
                    BasisKey = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxRuleBracket", x => new { x.RuleId, x.Version, x.Ordinal });
                    table.CheckConstraint("CK_TaxRuleBracket_Ordinal",
                        "typeof(Ordinal) = 'integer' AND Ordinal BETWEEN 0 AND 2147483647");
                    table.ForeignKey(
                        name: "FK_TaxRuleBracket_TaxRuleLegalBasis_BasisKey",
                        column: x => x.BasisKey,
                        principalTable: "TaxRuleLegalBasis",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaxRuleBracket_TaxRuleSnapshot_RuleId_Version",
                        columns: x => new { x.RuleId, x.Version },
                        principalTable: "TaxRuleSnapshot",
                        principalColumns: new[] { "RuleId", "Version" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaxRulePeriod",
                columns: table => new
                {
                    Key = table.Column<Guid>(type: "TEXT", nullable: false),
                    RuleId = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<string>(type: "TEXT", nullable: false),
                    PeriodFrom = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    PeriodTo = table.Column<DateOnly>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxRulePeriod", x => x.Key);
                    table.CheckConstraint("CK_TaxRulePeriod_Dates",
                        "length(PeriodFrom) = 10 AND " +
                        "length(PeriodTo) = 10 AND " +
                        "strftime('%Y-%m-%d', PeriodFrom, '+0 days') IS PeriodFrom AND " +
                        "strftime('%Y-%m-%d', PeriodTo, '+0 days') IS PeriodTo AND " +
                        "substr(PeriodFrom, 1, 4) BETWEEN '0001' AND '9999' AND " +
                        "substr(PeriodTo, 1, 4) BETWEEN '0001' AND '9999' AND " +
                        "substr(PeriodFrom, 9, 2) = '01' AND " +
                        "date(PeriodTo, '+1 day', 'start of month') IS date(PeriodTo, '+1 day') AND " +
                        "PeriodFrom <= PeriodTo");
                    table.ForeignKey(
                        name: "FK_TaxRulePeriod_TaxRuleSnapshot_RuleId_Version",
                        columns: x => new { x.RuleId, x.Version },
                        principalTable: "TaxRuleSnapshot",
                        principalColumns: new[] { "RuleId", "Version" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                CREATE TRIGGER TaxRulePeriod_NoOverlap_Insert BEFORE INSERT ON TaxRulePeriod
                WHEN EXISTS (SELECT 1 FROM TaxRulePeriod p
                    WHERE NEW.PeriodFrom <= p.PeriodTo AND NEW.PeriodTo >= p.PeriodFrom)
                BEGIN SELECT RAISE(ABORT, 'Tax rule periods overlap'); END;
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER TaxRulePeriod_NoOverlap_Update BEFORE UPDATE ON TaxRulePeriod
                WHEN EXISTS (SELECT 1 FROM TaxRulePeriod p
                    WHERE p.Key <> OLD.Key AND NEW.PeriodFrom <= p.PeriodTo AND NEW.PeriodTo >= p.PeriodFrom)
                BEGIN SELECT RAISE(ABORT, 'Tax rule periods overlap'); END;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_TaxRuleBracket_BasisKey",
                table: "TaxRuleBracket",
                column: "BasisKey");

            migrationBuilder.CreateIndex(
                name: "IX_TaxRulePeriod_PeriodFrom_PeriodTo",
                table: "TaxRulePeriod",
                columns: new[] { "PeriodFrom", "PeriodTo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxRulePeriod_RuleId_Version",
                table: "TaxRulePeriod",
                columns: new[] { "RuleId", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxRuleSnapshot_DependentBasisKey",
                table: "TaxRuleSnapshot",
                column: "DependentBasisKey");

            migrationBuilder.CreateIndex(
                name: "IX_TaxRuleSnapshot_IncomeBasisKey",
                table: "TaxRuleSnapshot",
                column: "IncomeBasisKey");

            migrationBuilder.CreateIndex(
                name: "IX_TaxRuleSnapshot_PersonalBasisKey",
                table: "TaxRuleSnapshot",
                column: "PersonalBasisKey");

            migrationBuilder.CreateIndex(
                name: "IX_TaxRuleSnapshot_TaxBasisKey",
                table: "TaxRuleSnapshot",
                column: "TaxBasisKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaxRuleBracket");

            migrationBuilder.DropTable(
                name: "TaxRulePeriod");

            migrationBuilder.DropTable(
                name: "TaxRuleSnapshot");

            migrationBuilder.DropTable(
                name: "TaxRuleLegalBasis");
        }
    }
}
