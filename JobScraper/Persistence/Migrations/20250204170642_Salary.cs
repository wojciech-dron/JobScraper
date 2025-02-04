using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobScraper.Migrations
{
    /// <inheritdoc />
    public partial class Salary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_JobOffers_Salary",
                table: "JobOffers");

            migrationBuilder.DropColumn(
                name: "Salary",
                table: "JobOffers");

            migrationBuilder.AlterColumn<string>(
                name: "Origin",
                table: "JobOffers",
                type: "TEXT",
                maxLength: 24,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 24);

            migrationBuilder.AddColumn<string>(
                name: "SalaryCurrency",
                table: "JobOffers",
                type: "TEXT",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SalaryMaxMonth",
                table: "JobOffers",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SalaryMinMonth",
                table: "JobOffers",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobOffers_SalaryCurrency",
                table: "JobOffers",
                column: "SalaryCurrency");

            migrationBuilder.CreateIndex(
                name: "IX_JobOffers_SalaryMaxMonth",
                table: "JobOffers",
                column: "SalaryMaxMonth");

            migrationBuilder.CreateIndex(
                name: "IX_JobOffers_SalaryMinMonth",
                table: "JobOffers",
                column: "SalaryMinMonth");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_JobOffers_SalaryCurrency",
                table: "JobOffers");

            migrationBuilder.DropIndex(
                name: "IX_JobOffers_SalaryMaxMonth",
                table: "JobOffers");

            migrationBuilder.DropIndex(
                name: "IX_JobOffers_SalaryMinMonth",
                table: "JobOffers");

            migrationBuilder.DropColumn(
                name: "SalaryCurrency",
                table: "JobOffers");

            migrationBuilder.DropColumn(
                name: "SalaryMaxMonth",
                table: "JobOffers");

            migrationBuilder.DropColumn(
                name: "SalaryMinMonth",
                table: "JobOffers");

            migrationBuilder.AlterColumn<string>(
                name: "Origin",
                table: "JobOffers",
                type: "TEXT",
                maxLength: 24,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 24,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Salary",
                table: "JobOffers",
                type: "TEXT",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobOffers_Salary",
                table: "JobOffers",
                column: "Salary");
        }
    }
}
