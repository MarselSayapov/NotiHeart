using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotiHeart.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "notifications",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Channel = table.Column<string>(type: "text", nullable: false),
                Recipient = table.Column<string>(type: "text", nullable: false),
                Text = table.Column<string>(type: "text", nullable: false),
                Status = table.Column<string>(type: "text", nullable: false),
                LastError = table.Column<string>(type: "text", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_notifications", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "notification_attachments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                NotificationId = table.Column<Guid>(type: "uuid", nullable: false),
                FileName = table.Column<string>(type: "text", nullable: false),
                ContentType = table.Column<string>(type: "text", nullable: false),
                Content = table.Column<byte[]>(type: "bytea", nullable: false),
                Size = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_notification_attachments", x => x.Id);
                table.ForeignKey(
                    name: "FK_notification_attachments_notifications_NotificationId",
                    column: x => x.NotificationId,
                    principalTable: "notifications",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "notification_attempts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                NotificationId = table.Column<Guid>(type: "uuid", nullable: false),
                AttemptNo = table.Column<int>(type: "integer", nullable: false),
                StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                FinishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                Result = table.Column<string>(type: "text", nullable: false),
                Error = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_notification_attempts", x => x.Id);
                table.ForeignKey(
                    name: "FK_notification_attempts_notifications_NotificationId",
                    column: x => x.NotificationId,
                    principalTable: "notifications",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_notification_attachments_NotificationId",
            table: "notification_attachments",
            column: "NotificationId");

        migrationBuilder.CreateIndex(
            name: "IX_notification_attempts_NotificationId",
            table: "notification_attempts",
            column: "NotificationId");

        migrationBuilder.CreateIndex(
            name: "IX_notifications_CreatedAt",
            table: "notifications",
            column: "CreatedAt");

        migrationBuilder.CreateIndex(
            name: "IX_notifications_Status",
            table: "notifications",
            column: "Status");
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
