using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobScraper.Migrations
{
    /// <inheritdoc />
    public partial class Ownable_Config : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ScraperConfigs",
                table: "ScraperConfigs");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "ScraperConfigs");

            migrationBuilder.AddColumn<string>(
                name: "Owner",
                table: "ScraperConfigs",
                type: "TEXT",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ScraperConfigs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ScraperConfigs",
                table: "ScraperConfigs",
                column: "Owner");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ScraperConfigs",
                table: "ScraperConfigs");

            migrationBuilder.DropColumn(
                name: "Owner",
                table: "ScraperConfigs");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ScraperConfigs");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "ScraperConfigs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0)
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ScraperConfigs",
                table: "ScraperConfigs",
                column: "Id");
        }
    }
}
