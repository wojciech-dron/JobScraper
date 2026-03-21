using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobScraper.Migrations
{
    /// <inheritdoc />
    public partial class CustomScraperConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Data migration: convert DataOrigin integer enum values to strings in SourcesJson
            // Enum values: 0=Manual, 1=Indeed, 2=JustJoinIt, 3=NoFluffJobs, 4=LinkedIn, 5=PracujPl, 6=RocketJobs, 7=Olx
            migrationBuilder.Sql("UPDATE ScraperConfigs SET SourcesJson = REPLACE(SourcesJson, '\"DataOrigin\":0', '\"DataOrigin\":\"Manual\"') WHERE SourcesJson LIKE '%\"DataOrigin\":0%'");
            migrationBuilder.Sql("UPDATE ScraperConfigs SET SourcesJson = REPLACE(SourcesJson, '\"DataOrigin\":1', '\"DataOrigin\":\"Indeed\"') WHERE SourcesJson LIKE '%\"DataOrigin\":1%'");
            migrationBuilder.Sql("UPDATE ScraperConfigs SET SourcesJson = REPLACE(SourcesJson, '\"DataOrigin\":2', '\"DataOrigin\":\"JustJoinIt\"') WHERE SourcesJson LIKE '%\"DataOrigin\":2%'");
            migrationBuilder.Sql("UPDATE ScraperConfigs SET SourcesJson = REPLACE(SourcesJson, '\"DataOrigin\":3', '\"DataOrigin\":\"NoFluffJobs\"') WHERE SourcesJson LIKE '%\"DataOrigin\":3%'");
            migrationBuilder.Sql("UPDATE ScraperConfigs SET SourcesJson = REPLACE(SourcesJson, '\"DataOrigin\":4', '\"DataOrigin\":\"LinkedIn\"') WHERE SourcesJson LIKE '%\"DataOrigin\":4%'");
            migrationBuilder.Sql("UPDATE ScraperConfigs SET SourcesJson = REPLACE(SourcesJson, '\"DataOrigin\":5', '\"DataOrigin\":\"PracujPl\"') WHERE SourcesJson LIKE '%\"DataOrigin\":5%'");
            migrationBuilder.Sql("UPDATE ScraperConfigs SET SourcesJson = REPLACE(SourcesJson, '\"DataOrigin\":6', '\"DataOrigin\":\"RocketJobs\"') WHERE SourcesJson LIKE '%\"DataOrigin\":6%'");
            migrationBuilder.Sql("UPDATE ScraperConfigs SET SourcesJson = REPLACE(SourcesJson, '\"DataOrigin\":7', '\"DataOrigin\":\"Olx\"') WHERE SourcesJson LIKE '%\"DataOrigin\":7%'");

            migrationBuilder.CreateTable(
                name: "CustomScraperConfigs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Owner = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DataOrigin = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ListScraperScript = table.Column<string>(type: "TEXT", maxLength: 10000, nullable: false),
                    DetailsScrapingEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    DetailsScraperScript = table.Column<string>(type: "TEXT", maxLength: 10000, nullable: true),
                    PaginationScript = table.Column<string>(type: "TEXT", maxLength: 10000, nullable: true),
                    Domain = table.Column<string>(type: "TEXT", maxLength: 253, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomScraperConfigs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomScraperConfigs");
        }
    }
}
