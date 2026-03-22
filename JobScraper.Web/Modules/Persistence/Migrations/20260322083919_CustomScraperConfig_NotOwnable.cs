using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobScraper.Migrations
{
    /// <inheritdoc />
    public partial class CustomScraperConfig_NotOwnable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Owner",
                table: "CustomScraperConfigs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
           migrationBuilder.AddColumn<string>(
                name: "Owner",
                table: "CustomScraperConfigs",
                type: "TEXT",
                maxLength: 255,
                nullable: true);
        }
    }
}
