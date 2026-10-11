using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobHunt.Api.Features.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchScores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FitScore",
                table: "Applications",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MatchSummary",
                table: "Applications",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OverallScore",
                table: "Applications",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SeniorityScore",
                table: "Applications",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SimilarityScore",
                table: "Applications",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SkillsScore",
                table: "Applications",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FitScore",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "MatchSummary",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "OverallScore",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "SeniorityScore",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "SimilarityScore",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "SkillsScore",
                table: "Applications");
        }
    }
}
