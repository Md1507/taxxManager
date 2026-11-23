using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaxManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDataProtectionAndAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Direct upgrade is allowed only when all sensitive tables are empty.
            // Populated legacy databases must use the explicit copy-forward upgrader.
            migrationBuilder.Sql("""
                CREATE TEMP TABLE RequireCopyForwardForPopulatedDatabase (N INTEGER CHECK (N = 0));
                INSERT INTO RequireCopyForwardForPopulatedDatabase SELECT
                    (SELECT count(*) FROM UserProfiles) + (SELECT count(*) FROM MonthlyIncomes) +
                    (SELECT count(*) FROM Dependents) + (SELECT count(*) FROM DependentEligibilityPeriods);
                DROP TABLE RequireCopyForwardForPopulatedDatabase;
                DROP TRIGGER Period_NoOverlap_Insert;
                DROP TRIGGER Period_NoOverlap_Update;
                DROP TABLE DependentEligibilityPeriods;
                DROP TABLE MonthlyIncomes;
                DROP TABLE Dependents;
                DROP TABLE UserProfiles;
                CREATE TABLE UserProfiles (
                    Id TEXT NOT NULL CONSTRAINT PK_UserProfiles PRIMARY KEY,
                    DisplayNameEncrypted BLOB NOT NULL);
                CREATE TABLE MonthlyIncomes (
                    ProfileId TEXT NOT NULL, Year INTEGER NOT NULL, Month INTEGER NOT NULL,
                    GrossIncomeEncrypted BLOB NOT NULL, InsuranceAmountEncrypted BLOB NOT NULL,
                    ActualWithheldTaxEncrypted BLOB NOT NULL,
                    CONSTRAINT PK_MonthlyIncomes PRIMARY KEY (ProfileId, Year, Month),
                    CONSTRAINT CK_Income_Year CHECK (Year BETWEEN 1 AND 9999),
                    CONSTRAINT CK_Income_Month CHECK (Month BETWEEN 1 AND 12),
                    CONSTRAINT FK_MonthlyIncomes_UserProfiles_ProfileId FOREIGN KEY (ProfileId) REFERENCES UserProfiles(Id) ON DELETE CASCADE);
                CREATE TABLE Dependents (
                    ProfileId TEXT NOT NULL, Id TEXT NOT NULL, DisplayNameEncrypted BLOB NOT NULL,
                    CONSTRAINT PK_Dependents PRIMARY KEY (ProfileId, Id),
                    CONSTRAINT FK_Dependents_UserProfiles_ProfileId FOREIGN KEY (ProfileId) REFERENCES UserProfiles(Id) ON DELETE CASCADE);
                CREATE TABLE DependentEligibilityPeriods (
                    ProfileId TEXT NOT NULL, DependentId TEXT NOT NULL, EligibleFromMonth TEXT NOT NULL, EligibleToMonth TEXT NOT NULL,
                    CONSTRAINT PK_DependentEligibilityPeriods PRIMARY KEY (ProfileId, DependentId, EligibleFromMonth),
                    CONSTRAINT FK_DependentEligibilityPeriods_Dependents_ProfileId_DependentId FOREIGN KEY (ProfileId, DependentId)
                        REFERENCES Dependents(ProfileId, Id) ON DELETE CASCADE,
                    CONSTRAINT CK_Period_Dates CHECK (
                        length(EligibleFromMonth) = 10 AND length(EligibleToMonth) = 10 AND
                        EligibleFromMonth GLOB '[0-9][0-9][0-9][0-9]-[0-9][0-9]-01' AND
                        EligibleToMonth GLOB '[0-9][0-9][0-9][0-9]-[0-9][0-9]-01' AND
                        substr(EligibleFromMonth,1,4) BETWEEN '0001' AND '9999' AND
                        substr(EligibleToMonth,1,4) BETWEEN '0001' AND '9999' AND
                        substr(EligibleFromMonth,6,2) BETWEEN '01' AND '12' AND
                        substr(EligibleToMonth,6,2) BETWEEN '01' AND '12' AND EligibleFromMonth <= EligibleToMonth));
                """);
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
            migrationBuilder.CreateTable(
                name: "AuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OperationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Ordinal = table.Column<int>(type: "INTEGER", nullable: false),
                    ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Entity = table.Column<string>(type: "TEXT", nullable: false),
                    EntityKey = table.Column<string>(type: "TEXT", nullable: false),
                    Action = table.Column<string>(type: "TEXT", nullable: false),
                    TimestampUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Payload = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataProtectionMetadata",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    DatabaseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    KeyCheck = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProtectionMetadata", x => x.Id);
                    table.CheckConstraint("CK_Protection_Singleton", "Id = 1");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_OperationId_Ordinal",
                table: "AuditEvents",
                columns: new[] { "OperationId", "Ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_ProfileId",
                table: "AuditEvents",
                column: "ProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("Protected schema cannot be downgraded. Restore the legacy backup with its compatible application.");
        }
    }
}
