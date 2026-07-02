using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeddingManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UnifyFlowEvents_PromoteCustomEventsToRealEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Data migration (must run BEFORE dropping the column):
            // Promote every flow-local custom event into a real "Events" row so events have a
            // single source of truth. We reuse the custom event's own id as the new Event.Id, which
            // keeps RsvpResponses.AttendingEventIds valid with no remap. When a wedding already has a
            // real event with the same name (the unique index IX_Events_WeddingId_Name would block a
            // duplicate), we instead reference that existing event and remap responses onto it.
            // jsonb keys are camelCase (RsvpJson uses Web defaults); Guids serialize as lowercase
            // hyphenated strings, matching uuid::text.
            migrationBuilder.Sql(
                """
                DO $$
                DECLARE
                    f RECORD;
                    ce RECORD;
                    w_date timestamptz;
                    target_id uuid;
                    ce_id uuid;
                    ce_name text;
                    ce_start timestamptz;
                    ce_loc text;
                BEGIN
                    FOR f IN
                        SELECT "Id", "WeddingId"
                        FROM "InvitationFlows"
                        WHERE "CustomEvents" IS NOT NULL
                          AND jsonb_typeof("CustomEvents") = 'array'
                          AND jsonb_array_length("CustomEvents") > 0
                    LOOP
                        SELECT "Date" INTO w_date FROM "Weddings" WHERE "Id" = f."WeddingId";

                        FOR ce IN
                            SELECT value FROM "InvitationFlows" flow,
                                   jsonb_array_elements(flow."CustomEvents") AS value
                            WHERE flow."Id" = f."Id"
                        LOOP
                            ce_id    := (ce.value->>'id')::uuid;
                            ce_name  := left(COALESCE(NULLIF(trim(ce.value->>'name'), ''), 'Event'), 200);
                            ce_start := COALESCE((ce.value->>'startDate')::timestamptz, w_date, now());
                            ce_loc   := left(COALESCE(ce.value->>'location', ''), 200);

                            -- Reuse an existing real event with the same name, else create one.
                            SELECT "Id" INTO target_id
                            FROM "Events"
                            WHERE "WeddingId" = f."WeddingId" AND "Name" = ce_name
                            LIMIT 1;

                            IF target_id IS NULL THEN
                                INSERT INTO "Events"
                                    ("Id", "Name", "StartDate", "EndDate", "Location", "Description", "WeddingId")
                                VALUES
                                    (ce_id, ce_name, ce_start, NULL, ce_loc, NULL, f."WeddingId")
                                ON CONFLICT ("Id") DO NOTHING;
                                target_id := ce_id;
                            END IF;

                            -- Ensure the flow exposes the target event.
                            UPDATE "InvitationFlows"
                            SET "EventIds" = (
                                SELECT jsonb_agg(DISTINCT e)
                                FROM (
                                    SELECT jsonb_array_elements("EventIds") AS e
                                    FROM "InvitationFlows" WHERE "Id" = f."Id"
                                    UNION
                                    SELECT to_jsonb(target_id::text)
                                ) s
                            )
                            WHERE "Id" = f."Id";

                            -- Name-collision path only: point existing responses at the merged event.
                            IF target_id <> ce_id THEN
                                UPDATE "RsvpResponses"
                                SET "AttendingEventIds" = (
                                    SELECT jsonb_agg(DISTINCT
                                        CASE WHEN elem = to_jsonb(ce_id::text)
                                             THEN to_jsonb(target_id::text)
                                             ELSE elem END)
                                    FROM jsonb_array_elements("AttendingEventIds") AS elem
                                )
                                WHERE "AttendingEventIds" @> to_jsonb(ARRAY[ce_id::text]);
                            END IF;
                        END LOOP;
                    END LOOP;
                END $$;
                """);

            migrationBuilder.DropColumn(
                name: "CustomEvents",
                table: "InvitationFlows");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Re-adds the column as an empty jsonb array. Promoted events remain as real Events rows
            // referenced via EventIds; the original flow-local payload is not reconstructed.
            migrationBuilder.AddColumn<string>(
                name: "CustomEvents",
                table: "InvitationFlows",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");
        }
    }
}
