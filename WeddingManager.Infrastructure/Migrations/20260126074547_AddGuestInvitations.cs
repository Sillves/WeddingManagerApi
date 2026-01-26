using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeddingManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGuestInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "InvitationSentAt",
                table: "Guests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvitationToken",
                table: "Guests",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InvitationTokenExpiresAt",
                table: "Guests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredLanguage",
                table: "Guests",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "en");

            migrationBuilder.CreateIndex(
                name: "IX_Guests_InvitationToken",
                table: "Guests",
                column: "InvitationToken",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Guests_InvitationToken",
                table: "Guests");

            migrationBuilder.DropColumn(
                name: "InvitationSentAt",
                table: "Guests");

            migrationBuilder.DropColumn(
                name: "InvitationToken",
                table: "Guests");

            migrationBuilder.DropColumn(
                name: "InvitationTokenExpiresAt",
                table: "Guests");

            migrationBuilder.DropColumn(
                name: "PreferredLanguage",
                table: "Guests");
        }
    }
}
