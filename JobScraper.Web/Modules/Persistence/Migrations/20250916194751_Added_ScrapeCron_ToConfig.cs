using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobScraper.Migrations
{
    /// <inheritdoc />
    public partial class Added_ScrapeCron_ToConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ScrapeCron",
                table: "ScraperConfigs",
                type: "TEXT",
                maxLength: 256,
                nullable: false,
                defaultValue: "0 15 * * *"); // every day at 15:00
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScrapeCron",
                table: "ScraperConfigs");
        }
    }
}
