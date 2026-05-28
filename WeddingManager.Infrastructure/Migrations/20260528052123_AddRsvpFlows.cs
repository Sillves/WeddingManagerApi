using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeddingManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRsvpFlows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Guests_WeddingId_Email",
                table: "Guests");

            migrationBuilder.AddColumn<string>(
                name: "Dietary",
                table: "Guests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PlusOneOfGuestId",
                table: "Guests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Surname",
                table: "Guests",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            // Backfill so the composite dedupe index treats legacy rows deterministically.
            migrationBuilder.Sql("UPDATE \"Guests\" SET \"Surname\" = '' WHERE \"Surname\" IS NULL;");

            migrationBuilder.CreateTable(
                name: "InvitationFlows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WeddingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Passcode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IncludePlusOne = table.Column<bool>(type: "boolean", nullable: false),
                    CustomQuestions = table.Column<string>(type: "jsonb", nullable: false),
                    EventIds = table.Column<string>(type: "jsonb", nullable: false),
                    CustomEvents = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvitationFlows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvitationFlows_Weddings_WeddingId",
                        column: x => x.WeddingId,
                        principalTable: "Weddings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RsvpResponses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WeddingId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvitationFlowId = table.Column<Guid>(type: "uuid", nullable: false),
                    GuestId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AttendingEventIds = table.Column<string>(type: "jsonb", nullable: false),
                    CustomAnswers = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RsvpResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RsvpResponses_Guests_GuestId",
                        column: x => x.GuestId,
                        principalTable: "Guests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RsvpResponses_InvitationFlows_InvitationFlowId",
                        column: x => x.InvitationFlowId,
                        principalTable: "InvitationFlows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RsvpResponses_Weddings_WeddingId",
                        column: x => x.WeddingId,
                        principalTable: "Weddings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Guests_PlusOneOfGuestId",
                table: "Guests",
                column: "PlusOneOfGuestId");

            migrationBuilder.CreateIndex(
                name: "IX_InvitationFlows_WeddingId",
                table: "InvitationFlows",
                column: "WeddingId");

            migrationBuilder.CreateIndex(
                name: "IX_InvitationFlows_WeddingId_Passcode",
                table: "InvitationFlows",
                columns: new[] { "WeddingId", "Passcode" },
                unique: true,
                filter: "\"Passcode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RsvpResponses_GuestId",
                table: "RsvpResponses",
                column: "GuestId");

            migrationBuilder.CreateIndex(
                name: "IX_RsvpResponses_InvitationFlowId",
                table: "RsvpResponses",
                column: "InvitationFlowId");

            migrationBuilder.CreateIndex(
                name: "IX_RsvpResponses_WeddingId_InvitationFlowId",
                table: "RsvpResponses",
                columns: new[] { "WeddingId", "InvitationFlowId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Guests_Guests_PlusOneOfGuestId",
                table: "Guests",
                column: "PlusOneOfGuestId",
                principalTable: "Guests",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Case-insensitive composite dedupe key. Functional (lower(...)) so it can't be
            // expressed via the fluent API — created here as raw SQL. COALESCE keeps null
            // surnames from defeating the constraint.
            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX \"IX_Guests_Wedding_Email_Name_Surname_ci\" " +
                "ON \"Guests\" (\"WeddingId\", lower(\"Email\"), lower(\"Name\"), lower(COALESCE(\"Surname\", '')));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Guests_Wedding_Email_Name_Surname_ci\";");

            migrationBuilder.DropForeignKey(
                name: "FK_Guests_Guests_PlusOneOfGuestId",
                table: "Guests");

            migrationBuilder.DropTable(
                name: "RsvpResponses");

            migrationBuilder.DropTable(
                name: "InvitationFlows");

            migrationBuilder.DropIndex(
                name: "IX_Guests_PlusOneOfGuestId",
                table: "Guests");

            migrationBuilder.DropColumn(
                name: "Dietary",
                table: "Guests");

            migrationBuilder.DropColumn(
                name: "PlusOneOfGuestId",
                table: "Guests");

            migrationBuilder.DropColumn(
                name: "Surname",
                table: "Guests");

            migrationBuilder.CreateIndex(
                name: "IX_Guests_WeddingId_Email",
                table: "Guests",
                columns: new[] { "WeddingId", "Email" },
                unique: true);
        }
    }
}
