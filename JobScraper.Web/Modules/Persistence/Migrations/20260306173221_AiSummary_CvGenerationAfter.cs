using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobScraper.Migrations
{
    /// <inheritdoc />
    public partial class AiSummary_CvGenerationAfter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ProviderName",
                table: "AiSummaryConfigs",
                newName: "DefaultAiModel");

            migrationBuilder.AddColumn<bool>(
                name: "CvGenerationEnabled",
                table: "AiSummaryConfigs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "DefaultCvId",
                table: "AiSummaryConfigs",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SmartAiModel",
                table: "AiSummaryConfigs",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiSummaryConfigs_DefaultCvId",
                table: "AiSummaryConfigs",
                column: "DefaultCvId");

            migrationBuilder.AddForeignKey(
                name: "FK_AiSummaryConfigs_Cvs_DefaultCvId",
                table: "AiSummaryConfigs",
                column: "DefaultCvId",
                principalTable: "Cvs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AiSummaryConfigs_Cvs_DefaultCvId",
                table: "AiSummaryConfigs");

            migrationBuilder.DropIndex(
                name: "IX_AiSummaryConfigs_DefaultCvId",
                table: "AiSummaryConfigs");

            migrationBuilder.DropColumn(
                name: "CvGenerationEnabled",
                table: "AiSummaryConfigs");

            migrationBuilder.DropColumn(
                name: "DefaultCvId",
                table: "AiSummaryConfigs");

            migrationBuilder.DropColumn(
                name: "SmartAiModel",
                table: "AiSummaryConfigs");

            migrationBuilder.RenameColumn(
                name: "DefaultAiModel",
                table: "AiSummaryConfigs",
                newName: "ProviderName");
        }
    }
}
