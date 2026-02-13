using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeddingManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWeddingUserPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanAccessEvents",
                table: "WeddingUsers",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "CanAccessExpenses",
                table: "WeddingUsers",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "CanAccessGuests",
                table: "WeddingUsers",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "CanAccessWebsite",
                table: "WeddingUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsReadOnly",
                table: "WeddingUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanAccessEvents",
                table: "WeddingUsers");

            migrationBuilder.DropColumn(
                name: "CanAccessExpenses",
                table: "WeddingUsers");

            migrationBuilder.DropColumn(
                name: "CanAccessGuests",
                table: "WeddingUsers");

            migrationBuilder.DropColumn(
                name: "CanAccessWebsite",
                table: "WeddingUsers");

            migrationBuilder.DropColumn(
                name: "IsReadOnly",
                table: "WeddingUsers");
        }
    }
}
