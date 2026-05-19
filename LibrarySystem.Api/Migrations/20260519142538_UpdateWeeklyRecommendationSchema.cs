using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibrarySystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateWeeklyRecommendationSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "WeekStartUtc",
                table: "WeeklyRecommendations",
                newName: "WeekStartDate");

            migrationBuilder.AddColumn<string>(
                name: "AuthorName",
                table: "WeeklyRecommendations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "WeekEndDate",
                table: "WeeklyRecommendations",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.Sql(
                """
                UPDATE wr
                SET WeekEndDate = DATEADD(day, 6, wr.WeekStartDate),
                    AuthorName = LTRIM(RTRIM(u.FirstName + ' ' + u.LastName))
                FROM WeeklyRecommendations wr
                INNER JOIN Users u ON u.Id = wr.AuthorUserId
                """);

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyRecommendations_WeekStartDate_WeekEndDate",
                table: "WeeklyRecommendations",
                columns: new[] { "WeekStartDate", "WeekEndDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WeeklyRecommendations_WeekStartDate_WeekEndDate",
                table: "WeeklyRecommendations");

            migrationBuilder.DropColumn(
                name: "AuthorName",
                table: "WeeklyRecommendations");

            migrationBuilder.DropColumn(
                name: "WeekEndDate",
                table: "WeeklyRecommendations");

            migrationBuilder.RenameColumn(
                name: "WeekStartDate",
                table: "WeeklyRecommendations",
                newName: "WeekStartUtc");
        }
    }
}
