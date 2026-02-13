using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeddingManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BackfillWeddingUserOwners : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO ""WeddingUsers"" (""WeddingId"", ""UserId"", ""Role"", ""AddedAt"", ""CanAccessGuests"", ""CanAccessEvents"", ""CanAccessExpenses"", ""CanAccessWebsite"", ""IsReadOnly"")
                SELECT w.""Id"", w.""UserId"", 'Owner', NOW(), true, true, true, true, false
                FROM ""Weddings"" w
                WHERE NOT EXISTS (
                    SELECT 1 FROM ""WeddingUsers"" wu
                    WHERE wu.""WeddingId"" = w.""Id"" AND wu.""UserId"" = w.""UserId""
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty: cannot reliably reverse a data backfill
            // without risking removal of legitimately-created WeddingUser records.
        }
    }
}
