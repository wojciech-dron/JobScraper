using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobScraper.Migrations
{
    /// <inheritdoc />
    public partial class More_General_ExpectedSalary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ExpectedMonthSalary",
                table: "Applications",
                newName: "ExpectedSalary");

            migrationBuilder.RenameIndex(
                name: "IX_Applications_ExpectedMonthSalary",
                table: "Applications",
                newName: "IX_Applications_ExpectedSalary");

            migrationBuilder.AddColumn<string>(
                name: "ExpectedSalaryCurrency",
                table: "Applications",
                type: "TEXT",
                maxLength: 10,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpectedSalaryCurrency",
                table: "Applications");

            migrationBuilder.RenameColumn(
                name: "ExpectedSalary",
                table: "Applications",
                newName: "ExpectedMonthSalary");

            migrationBuilder.RenameIndex(
                name: "IX_Applications_ExpectedSalary",
                table: "Applications",
                newName: "IX_Applications_ExpectedMonthSalary");
        }
    }
}
