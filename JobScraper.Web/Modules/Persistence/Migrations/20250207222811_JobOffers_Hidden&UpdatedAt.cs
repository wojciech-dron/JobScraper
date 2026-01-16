using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobScraper.Migrations
{
    /// <inheritdoc />
    public partial class JobOffers_HiddenUpdatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Hidden",
                table: "JobOffers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "JobOffers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobOffers_Hidden",
                table: "JobOffers",
                column: "Hidden");

            migrationBuilder.CreateIndex(
                name: "IX_JobOffers_UpdatedAt",
                table: "JobOffers",
                column: "UpdatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_JobOffers_Hidden",
                table: "JobOffers");

            migrationBuilder.DropIndex(
                name: "IX_JobOffers_UpdatedAt",
                table: "JobOffers");

            migrationBuilder.DropColumn(
                name: "Hidden",
                table: "JobOffers");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "JobOffers");
        }
    }
}
