using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobScraper.Migrations
{
    /// <inheritdoc />
    public partial class Fix_Application_OfferUrl_Index : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (!migrationBuilder.ActiveProvider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
                return;

            // somehow ef have problems with creating non-unique indexes for sqlite
            migrationBuilder.Sql("""
                                 DROP INDEX IF EXISTS "IX_Applications_OfferUrl";
                                 """);

            migrationBuilder.Sql("""
                                 CREATE INDEX "IX_Applications_OfferUrl" ON "Applications" ("OfferUrl");
                                 """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
