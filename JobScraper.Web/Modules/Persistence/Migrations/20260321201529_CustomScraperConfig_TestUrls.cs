using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobScraper.Migrations
{
    /// <inheritdoc />
    public partial class CustomScraperConfig_TestUrls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TestDetailsUrl",
                table: "CustomScraperConfigs",
                type: "TEXT",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TestListUrl",
                table: "CustomScraperConfigs",
                type: "TEXT",
                maxLength: 2048,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TestDetailsUrl",
                table: "CustomScraperConfigs");

            migrationBuilder.DropColumn(
                name: "TestListUrl",
                table: "CustomScraperConfigs");
        }
    }
}
