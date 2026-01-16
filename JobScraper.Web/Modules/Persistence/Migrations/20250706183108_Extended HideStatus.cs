using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobScraper.Migrations
{
    /// <inheritdoc />
    public partial class ExtendedHideStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Hidden",
                table: "JobOffers",
                newName: "HideStatus");

            migrationBuilder.RenameIndex(
                name: "IX_JobOffers_Hidden",
                table: "JobOffers",
                newName: "IX_JobOffers_HideStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "HideStatus",
                table: "JobOffers",
                newName: "Hidden");

            migrationBuilder.RenameIndex(
                name: "IX_JobOffers_HideStatus",
                table: "JobOffers",
                newName: "IX_JobOffers_Hidden");
        }
    }
}
