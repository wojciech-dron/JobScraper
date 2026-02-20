using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobScraper.Migrations
{
    /// <inheritdoc />
    public partial class Added_AiSummaryConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiSummaryConfigs",
                columns: table => new
                {
                    Owner = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    AiSummaryEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    ProviderName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CvContent = table.Column<string>(type: "TEXT", maxLength: 10000, nullable: false),
                    UserRequirements = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    TestOfferContent = table.Column<string>(type: "TEXT", maxLength: 10000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiSummaryConfigs", x => x.Owner);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiSummaryConfigs");
        }
    }
}
