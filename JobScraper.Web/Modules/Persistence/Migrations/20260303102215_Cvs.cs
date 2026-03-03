using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobScraper.Migrations
{
    /// <inheritdoc />
    public partial class Cvs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CvId",
                table: "UserOffers",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Cvs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Owner = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    IsTemplate = table.Column<bool>(type: "INTEGER", nullable: false),
                    MarkdownContent = table.Column<string>(type: "TEXT", maxLength: 30000, nullable: false),
                    Disclaimer = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    OriginCvId = table.Column<long>(type: "INTEGER", nullable: true),
                    ChatHistory = table.Column<string>(type: "TEXT", nullable: true),
                    LayoutConfig = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cvs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cvs_Cvs_OriginCvId",
                        column: x => x.OriginCvId,
                        principalTable: "Cvs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserOffers_CvId",
                table: "UserOffers",
                column: "CvId");

            migrationBuilder.CreateIndex(
                name: "IX_Cvs_IsTemplate",
                table: "Cvs",
                column: "IsTemplate");

            migrationBuilder.CreateIndex(
                name: "IX_Cvs_OriginCvId",
                table: "Cvs",
                column: "OriginCvId");

            migrationBuilder.CreateIndex(
                name: "IX_Cvs_UpdatedAt",
                table: "Cvs",
                column: "UpdatedAt");

            migrationBuilder.AddForeignKey(
                name: "FK_UserOffers_Cvs_CvId",
                table: "UserOffers",
                column: "CvId",
                principalTable: "Cvs",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserOffers_Cvs_CvId",
                table: "UserOffers");

            migrationBuilder.DropTable(
                name: "Cvs");

            migrationBuilder.DropIndex(
                name: "IX_UserOffers_CvId",
                table: "UserOffers");

            migrationBuilder.DropColumn(
                name: "CvId",
                table: "UserOffers");
        }
    }
}
