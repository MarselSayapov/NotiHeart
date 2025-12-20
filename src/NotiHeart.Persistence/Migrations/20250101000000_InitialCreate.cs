using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotiHeart.Persistence.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "notifications",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                correlation_id = table.Column<string>(type: "text", nullable: false),
                channel = table.Column<string>(type: "text", nullable: false),
                recipient = table.Column<string>(type: "text", nullable: false),
                text = table.Column<string>(type: "text", nullable: false),
                metadata = table.Column<string>(type: "jsonb", nullable: true),
                status = table.Column<string>(type: "text", nullable: false),
                last_error = table.Column<string>(type: "text", nullable: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'")
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_notifications", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "notification_attachments",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                notification_id = table.Column<Guid>(type: "uuid", nullable: false),
                file_name = table.Column<string>(type: "text", nullable: false),
                content_type = table.Column<string>(type: "text", nullable: false),
                content = table.Column<byte[]>(type: "bytea", nullable: false),
                size = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_notification_attachments", x => x.id);
                table.ForeignKey(
                    name: "fk_notification_attachments_notifications_notification_id",
                    column: x => x.notification_id,
                    principalTable: "notifications",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "notification_attempts",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                notification_id = table.Column<Guid>(type: "uuid", nullable: false),
                attempt_no = table.Column<int>(type: "integer", nullable: false),
                result = table.Column<string>(type: "text", nullable: false),
                error = table.Column<string>(type: "text", nullable: true),
                started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_notification_attempts", x => x.id);
                table.ForeignKey(
                    name: "fk_notification_attempts_notifications_notification_id",
                    column: x => x.notification_id,
                    principalTable: "notifications",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_notification_attachments_notification_id",
            table: "notification_attachments",
            column: "notification_id");

        migrationBuilder.CreateIndex(
            name: "ix_notification_attempts_notification_id",
            table: "notification_attempts",
            column: "notification_id");

        migrationBuilder.CreateIndex(
            name: "ix_notifications_created_at",
            table: "notifications",
            column: "created_at");

        migrationBuilder.CreateIndex(
            name: "ix_notifications_status",
            table: "notifications",
            column: "status");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "notification_attachments");

        migrationBuilder.DropTable(
            name: "notification_attempts");

        migrationBuilder.DropTable(
            name: "notifications");
    }
}
