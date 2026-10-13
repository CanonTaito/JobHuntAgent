using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobHunt.Api.Features.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFollowUpDraft : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FollowUpBody",
                table: "Applications",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FollowUpDraftedAt",
                table: "Applications",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FollowUpSendAfterDays",
                table: "Applications",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FollowUpSubject",
                table: "Applications",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FollowUpBody",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "FollowUpDraftedAt",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "FollowUpSendAfterDays",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "FollowUpSubject",
                table: "Applications");
        }
    }
}
