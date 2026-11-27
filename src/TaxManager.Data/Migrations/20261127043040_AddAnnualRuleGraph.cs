using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaxManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAnnualRuleGraph : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AnnualGraph",
                table: "TaxRuleSnapshot",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnnualGraph",
                table: "TaxRuleSnapshot");
        }
    }
}
