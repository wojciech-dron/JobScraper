using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobScraper.Migrations
{
    /// <inheritdoc />
    public partial class Fix_Cv_FK_Behaviour : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cvs_Cvs_OriginCvId",
                table: "Cvs");

            migrationBuilder.DropForeignKey(
                name: "FK_UserOffers_Cvs_CvId",
                table: "UserOffers");

            migrationBuilder.AddForeignKey(
                name: "FK_Cvs_Cvs_OriginCvId",
                table: "Cvs",
                column: "OriginCvId",
                principalTable: "Cvs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_UserOffers_Cvs_CvId",
                table: "UserOffers",
                column: "CvId",
                principalTable: "Cvs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cvs_Cvs_OriginCvId",
                table: "Cvs");

            migrationBuilder.DropForeignKey(
                name: "FK_UserOffers_Cvs_CvId",
                table: "UserOffers");

            migrationBuilder.AddForeignKey(
                name: "FK_Cvs_Cvs_OriginCvId",
                table: "Cvs",
                column: "OriginCvId",
                principalTable: "Cvs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserOffers_Cvs_CvId",
                table: "UserOffers",
                column: "CvId",
                principalTable: "Cvs",
                principalColumn: "Id");
        }
    }
}
