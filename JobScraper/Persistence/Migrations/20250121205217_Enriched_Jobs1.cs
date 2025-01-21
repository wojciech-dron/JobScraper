using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobScraper.Migrations
{
    /// <inheritdoc />
    public partial class Enriched_Jobs1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Location",
                table: "JobOffers",
                type: "TEXT",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 255);

            migrationBuilder.AddColumn<string>(
                name: "DetailsScrapeStatus",
                table: "JobOffers",
                type: "TEXT",
                maxLength: 24,
                nullable: false,
                defaultValue: "ToScrape");

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAt",
                table: "JobOffers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Companies",
                type: "TEXT",
                maxLength: 30000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobOffers_DetailsScrapeStatus",
                table: "JobOffers",
                column: "DetailsScrapeStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_JobOffers_DetailsScrapeStatus",
                table: "JobOffers");

            migrationBuilder.DropColumn(
                name: "DetailsScrapeStatus",
                table: "JobOffers");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "JobOffers");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Companies");

            migrationBuilder.AlterColumn<string>(
                name: "Location",
                table: "JobOffers",
                type: "TEXT",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 100,
                oldNullable: true);
        }
    }
}
