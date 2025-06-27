using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobScraper.Migrations
{
    /// <inheritdoc />
    public partial class ScraperConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScraperConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WaitForListSeconds = table.Column<float>(type: "REAL", nullable: false),
                    WaitForScrollSeconds = table.Column<float>(type: "REAL", nullable: false),
                    WaitForDetailsSeconds = table.Column<float>(type: "REAL", nullable: false),
                    ShowBrowserWhenScraping = table.Column<bool>(type: "INTEGER", nullable: false),
                    BrowserType = table.Column<string>(type: "TEXT", nullable: false),
                    Keywords = table.Column<string>(type: "TEXT", nullable: false),
                    SourcesJson = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScraperConfigs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScraperConfigs");
        }
    }
}
