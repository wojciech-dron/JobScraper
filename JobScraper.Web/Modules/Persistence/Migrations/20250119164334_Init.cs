using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobScraper.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ScrapedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    IndeedUrl = table.Column<string>(type: "TEXT", nullable: true),
                    JjitUrl = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "JobOffers",
                columns: table => new
                {
                    OfferUrl = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Origin = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                    CompanyName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    Location = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    ScrapedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    OfferKeywords = table.Column<string>(type: "TEXT", nullable: false),
                    AgeInfo = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 5000, nullable: true),
                    ApplyUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    HtmlPath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    ScreenShotPath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    MyKeywords = table.Column<string>(type: "TEXT", nullable: false),
                    Salary = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobOffers", x => x.OfferUrl);
                    table.ForeignKey(
                        name: "FK_JobOffers_Companies_CompanyName",
                        column: x => x.CompanyName,
                        principalTable: "Companies",
                        principalColumn: "Name");
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobOffers_AgeInfo",
                table: "JobOffers",
                column: "AgeInfo");

            migrationBuilder.CreateIndex(
                name: "IX_JobOffers_CompanyName",
                table: "JobOffers",
                column: "CompanyName");

            migrationBuilder.CreateIndex(
                name: "IX_JobOffers_Location",
                table: "JobOffers",
                column: "Location");

            migrationBuilder.CreateIndex(
                name: "IX_JobOffers_Salary",
                table: "JobOffers",
                column: "Salary");

            migrationBuilder.CreateIndex(
                name: "IX_JobOffers_ScrapedAt",
                table: "JobOffers",
                column: "ScrapedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobOffers");

            migrationBuilder.DropTable(
                name: "Companies");
        }
    }
}
