using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobScraper.Migrations
{
    /// <inheritdoc />
    public partial class Added_Ownable_Offers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Owner",
                table: "Applications",
                type: "TEXT",
                maxLength: 255,
                nullable: false,
                defaultValue: "system", // Adds the constraint
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 255);

            migrationBuilder.AddColumn<string>(
                name: "ApplyUrl",
                table: "Applications",
                type: "TEXT",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserOffers",
                columns: table => new
                {
                    OfferUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Owner = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false, defaultValue: "system"),
                    HideStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    MyKeywords = table.Column<string>(type: "TEXT", nullable: false),
                    Comments = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserOffers", x => new { x.Owner, x.OfferUrl });
                });

            MigrateData(migrationBuilder);

            migrationBuilder.AddForeignKey(
                name: "FK_UserOffers_JobOffers_OfferUrl",
                table: "UserOffers",
                column: "OfferUrl",
                principalTable: "JobOffers",
                principalColumn: "OfferUrl",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.CreateIndex(
                name: "IX_UserOffers_HideStatus",
                table: "UserOffers",
                column: "HideStatus");

            migrationBuilder.CreateIndex(
                name: "IX_UserOffers_OfferUrl",
                table: "UserOffers",
                column: "OfferUrl");

            migrationBuilder.AddForeignKey(
                name: "FK_Applications_UserOffers_Owner_OfferUrl",
                table: "Applications",
                columns: new[] { "Owner", "OfferUrl" },
                principalTable: "UserOffers",
                principalColumns: new[] { "Owner", "OfferUrl" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.DropForeignKey(
                name: "FK_Applications_JobOffers_OfferUrl",
                table: "Applications");

            migrationBuilder.DropIndex(
                name: "IX_JobOffers_AgeInfo",
                table: "JobOffers");

            migrationBuilder.DropIndex(
                name: "IX_JobOffers_HideStatus",
                table: "JobOffers");

            migrationBuilder.DropIndex(
                name: "IX_Applications_OfferUrl",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "AgeInfo",
                table: "JobOffers");

            migrationBuilder.DropColumn(
                name: "ApplyUrl",
                table: "JobOffers");

            migrationBuilder.DropColumn(
                name: "Comments",
                table: "JobOffers");

            migrationBuilder.DropColumn(
                name: "HideStatus",
                table: "JobOffers");

            migrationBuilder.DropColumn(
                name: "MyKeywords",
                table: "JobOffers");
        }

        private void MigrateData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO UserOffers (
                    OfferUrl,
                    HideStatus,
                    MyKeywords,
                    Comments,
                    UpdatedAt,
                    Owner
                )
                SELECT
                    OfferUrl,
                    HideStatus,
                    MyKeywords,
                    Comments,
                    UpdatedAt,
                    'system'
                FROM JobOffers;
                """);

            migrationBuilder.Sql(
                """
                UPDATE Applications
                SET Owner = 'system'
                """);

            migrationBuilder.Sql(
                """
                UPDATE Applications
                SET
                    ApplyUrl = JobOffers.ApplyUrl
                FROM JobOffers
                WHERE Applications.OfferUrl = JobOffers.OfferUrl;
                """);
        }

        private void RollbackData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO JobOffers (
                    OfferUrl,
                    HideStatus,
                    MyKeywords,
                    Comments,
                    UpdatedAt
                )
                SELECT
                    OfferUrl,
                    HideStatus,
                    MyKeywords,
                    Comments,
                    UpdatedAt
                FROM UserOffers;
                """);

            migrationBuilder.Sql(
                """
                UPDATE JobOffers
                SET
                    ApplyUrl = Applications.ApplyUrl
                FROM Applications
                WHERE JobOffers.OfferUrl = Applications.OfferUrl;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
           migrationBuilder.AddColumn<string>(
                name: "AgeInfo",
                table: "JobOffers",
                type: "TEXT",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApplyUrl",
                table: "JobOffers",
                type: "TEXT",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Comments",
                table: "JobOffers",
                type: "TEXT",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "HideStatus",
                table: "JobOffers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "MyKeywords",
                table: "JobOffers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            RollbackData(migrationBuilder);

            migrationBuilder.CreateIndex(
                name: "IX_JobOffers_AgeInfo",
                table: "JobOffers",
                column: "AgeInfo");

            migrationBuilder.CreateIndex(
                name: "IX_JobOffers_HideStatus",
                table: "JobOffers",
                column: "HideStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_OfferUrl",
                table: "Applications",
                column: "OfferUrl",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Applications_JobOffers_OfferUrl",
                table: "Applications",
                column: "OfferUrl",
                principalTable: "JobOffers",
                principalColumn: "OfferUrl",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.DropForeignKey(
                name: "FK_Applications_UserOffers_Owner_OfferUrl",
                table: "Applications");

            migrationBuilder.DropTable(
                name: "UserOffers");

            migrationBuilder.DropColumn(
                name: "ApplyUrl",
                table: "Applications");
        }
    }
}
