using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobHunt.Api.Features.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTailoringDraft : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CoverLetter",
                table: "Applications",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TailoredAt",
                table: "Applications",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TailoredBullets",
                table: "Applications",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoverLetter",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "TailoredAt",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "TailoredBullets",
                table: "Applications");
        }
    }
}
