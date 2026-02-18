using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace JobScraper.Migrations
{
    /// <inheritdoc />
    public partial class Admin_AiSummary_Roles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AiSummary",
                table: "UserOffers",
                type: "TEXT",
                maxLength: 5000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiSummaryStatus",
                table: "UserOffers",
                type: "TEXT",
                maxLength: 12,
                nullable: true,
                defaultValue: "None");

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "6a427606-d71e-451e-9276-f363c43777f0", "019c70b8-19d0-715b-9595-2f4f60b20c03", "AiSummary", "AISUMMARY" },
                    { "6a427606-d71e-451e-9276-f363c43777f9", "019c70b8-19d0-715b-9595-2f4f60b20c02", "Admin", "ADMIN" }
                });

            migrationBuilder.Sql(
                """
                INSERT OR IGNORE INTO AspNetUserRoles (UserId, RoleId)
                SELECT u.Id, r.column1
                FROM (SELECT Id FROM AspNetUsers LIMIT 1) u
                CROSS JOIN (
                    VALUES
                        ('6a427606-d71e-451e-9276-f363c43777f0'),
                        ('6a427606-d71e-451e-9276-f363c43777f9')
                ) r;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM AspNetUserRoles
                WHERE RoleId IN ('6a427606-d71e-451e-9276-f363c43777f9', '6a427606-d71e-451e-9276-f363c43777f0')
                """);

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "6a427606-d71e-451e-9276-f363c43777f0");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "6a427606-d71e-451e-9276-f363c43777f9");

            migrationBuilder.DropColumn(
                name: "AiSummary",
                table: "UserOffers");

            migrationBuilder.DropColumn(
                name: "AiSummaryStatus",
                table: "UserOffers");
        }
    }
}
