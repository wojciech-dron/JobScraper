using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobScraper.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTickerQ : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TimeTickers_TimeTickers_BatchParent",
                schema: "jobs",
                table: "TimeTickers");

            migrationBuilder.RenameColumn(
                name: "Exception",
                schema: "jobs",
                table: "TimeTickers",
                newName: "SkippedReason");

            migrationBuilder.RenameColumn(
                name: "BatchRunCondition",
                schema: "jobs",
                table: "TimeTickers",
                newName: "RunCondition");

            migrationBuilder.RenameColumn(
                name: "BatchParent",
                schema: "jobs",
                table: "TimeTickers",
                newName: "ParentId");

            migrationBuilder.RenameIndex(
                name: "IX_TimeTickers_BatchParent",
                schema: "jobs",
                table: "TimeTickers",
                newName: "IX_TimeTickers_ParentId");

            migrationBuilder.RenameColumn(
                name: "Exception",
                schema: "jobs",
                table: "CronTickerOccurrences",
                newName: "SkippedReason");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ExecutionTime",
                schema: "jobs",
                table: "TimeTickers",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "ExceptionMessage",
                schema: "jobs",
                table: "TimeTickers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                schema: "jobs",
                table: "CronTickerOccurrences",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ExceptionMessage",
                schema: "jobs",
                table: "CronTickerOccurrences",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "jobs",
                table: "CronTickerOccurrences",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_Function_Expression",
                schema: "jobs",
                table: "CronTickers",
                columns: new[] { "Function", "Expression" });

            migrationBuilder.AddForeignKey(
                name: "FK_TimeTickers_TimeTickers_ParentId",
                schema: "jobs",
                table: "TimeTickers",
                column: "ParentId",
                principalSchema: "jobs",
                principalTable: "TimeTickers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TimeTickers_TimeTickers_ParentId",
                schema: "jobs",
                table: "TimeTickers");

            migrationBuilder.DropIndex(
                name: "IX_Function_Expression",
                schema: "jobs",
                table: "CronTickers");

            migrationBuilder.DropColumn(
                name: "ExceptionMessage",
                schema: "jobs",
                table: "TimeTickers");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "jobs",
                table: "CronTickerOccurrences");

            migrationBuilder.DropColumn(
                name: "ExceptionMessage",
                schema: "jobs",
                table: "CronTickerOccurrences");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "jobs",
                table: "CronTickerOccurrences");

            migrationBuilder.RenameColumn(
                name: "SkippedReason",
                schema: "jobs",
                table: "TimeTickers",
                newName: "Exception");

            migrationBuilder.RenameColumn(
                name: "RunCondition",
                schema: "jobs",
                table: "TimeTickers",
                newName: "BatchRunCondition");

            migrationBuilder.RenameColumn(
                name: "ParentId",
                schema: "jobs",
                table: "TimeTickers",
                newName: "BatchParent");

            migrationBuilder.RenameIndex(
                name: "IX_TimeTickers_ParentId",
                schema: "jobs",
                table: "TimeTickers",
                newName: "IX_TimeTickers_BatchParent");

            migrationBuilder.RenameColumn(
                name: "SkippedReason",
                schema: "jobs",
                table: "CronTickerOccurrences",
                newName: "Exception");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ExecutionTime",
                schema: "jobs",
                table: "TimeTickers",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TimeTickers_TimeTickers_BatchParent",
                schema: "jobs",
                table: "TimeTickers",
                column: "BatchParent",
                principalSchema: "jobs",
                principalTable: "TimeTickers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
