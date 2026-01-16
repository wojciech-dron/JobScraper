using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobScraper.Migrations
{
    /// <inheritdoc />
    public partial class AvoidKeywords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Keywords",
                table: "ScraperConfigs",
                newName: "MyKeywords");

            migrationBuilder.AddColumn<string>(
                name: "AvoidKeywords",
                table: "ScraperConfigs",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvoidKeywords",
                table: "ScraperConfigs");

            migrationBuilder.RenameColumn(
                name: "MyKeywords",
                table: "ScraperConfigs",
                newName: "Keywords");
        }
    }
}
