using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobScraper.Migrations
{
    /// <inheritdoc />
    public partial class CvImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Cvs",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<long>(
                name: "ImageId",
                table: "Cvs",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CvImages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Owner = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Size = table.Column<long>(type: "INTEGER", nullable: false),
                    Data = table.Column<byte[]>(type: "BLOB", maxLength: 5242880, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CvImages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cvs_CreatedAt",
                table: "Cvs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Cvs_ImageId",
                table: "Cvs",
                column: "ImageId");

            migrationBuilder.CreateIndex(
                name: "IX_CvImages_CreatedAt",
                table: "CvImages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CvImages_Owner",
                table: "CvImages",
                column: "Owner");

            migrationBuilder.CreateIndex(
                name: "IX_CvImages_UpdatedAt",
                table: "CvImages",
                column: "UpdatedAt");

            migrationBuilder.AddForeignKey(
                name: "FK_Cvs_CvImages_ImageId",
                table: "Cvs",
                column: "ImageId",
                principalTable: "CvImages",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cvs_CvImages_ImageId",
                table: "Cvs");

            migrationBuilder.DropTable(
                name: "CvImages");

            migrationBuilder.DropIndex(
                name: "IX_Cvs_CreatedAt",
                table: "Cvs");

            migrationBuilder.DropIndex(
                name: "IX_Cvs_ImageId",
                table: "Cvs");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Cvs");

            migrationBuilder.DropColumn(
                name: "ImageId",
                table: "Cvs");
        }
    }
}
