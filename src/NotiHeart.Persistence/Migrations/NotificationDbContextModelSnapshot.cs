using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NotiHeart.Contracts;

#nullable disable

namespace NotiHeart.Persistence.Migrations;

[DbContext(typeof(NotificationDbContext))]
partial class NotificationDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("uuid-ossp");

        modelBuilder.Entity("NotiHeart.Persistence.Notification", b =>
        {
            b.Property<Guid>("Id")
                .HasColumnType("uuid");

            b.Property<string>("Channel")
                .IsRequired()
                .HasColumnType("text");

            b.Property<string>("CorrelationId")
                .IsRequired()
                .HasColumnType("text");

            b.Property<DateTimeOffset>("CreatedAt")
                .ValueGeneratedOnAdd()
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now() at time zone 'utc'");

            b.Property<string>("LastError")
                .HasColumnType("text");

            b.Property<string>("Metadata")
                .HasColumnType("jsonb");

            b.Property<string>("Recipient")
                .IsRequired()
                .HasColumnType("text");

            b.Property<string>("Status")
                .IsRequired()
                .HasColumnType("text");

            b.Property<string>("Text")
                .IsRequired()
                .HasColumnType("text");

            b.Property<DateTimeOffset>("UpdatedAt")
                .ValueGeneratedOnAdd()
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now() at time zone 'utc'");

            b.HasKey("Id");

            b.HasIndex("CreatedAt");

            b.HasIndex("Status");

            b.ToTable("notifications");
        });

        modelBuilder.Entity("NotiHeart.Persistence.NotificationAttachment", b =>
        {
            b.Property<Guid>("Id")
                .HasColumnType("uuid");

            b.Property<byte[]>("Content")
                .IsRequired()
                .HasColumnType("bytea");

            b.Property<string>("ContentType")
                .IsRequired()
                .HasColumnType("text");

            b.Property<string>("FileName")
                .IsRequired()
                .HasColumnType("text");

            b.Property<Guid>("NotificationId")
                .HasColumnType("uuid");

            b.Property<long>("Size")
                .HasColumnType("bigint");

            b.HasKey("Id");

            b.HasIndex("NotificationId");

            b.ToTable("notification_attachments");
        });

        modelBuilder.Entity("NotiHeart.Persistence.NotificationAttempt", b =>
        {
            b.Property<Guid>("Id")
                .HasColumnType("uuid");

            b.Property<int>("AttemptNo")
                .HasColumnType("integer");

            b.Property<string>("Error")
                .HasColumnType("text");

            b.Property<DateTimeOffset?>("FinishedAt")
                .HasColumnType("timestamp with time zone");

            b.Property<Guid>("NotificationId")
                .HasColumnType("uuid");

            b.Property<string>("Result")
                .IsRequired()
                .HasColumnType("text");

            b.Property<DateTimeOffset>("StartedAt")
                .ValueGeneratedOnAdd()
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now() at time zone 'utc'");

            b.HasKey("Id");

            b.HasIndex("NotificationId");

            b.ToTable("notification_attempts");
        });

        modelBuilder.Entity("NotiHeart.Persistence.NotificationAttachment", b =>
        {
            b.HasOne("NotiHeart.Persistence.Notification", "Notification")
                .WithMany("Attachments")
                .HasForeignKey("NotificationId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.Navigation("Notification");
        });

        modelBuilder.Entity("NotiHeart.Persistence.NotificationAttempt", b =>
        {
            b.HasOne("NotiHeart.Persistence.Notification", "Notification")
                .WithMany("Attempts")
                .HasForeignKey("NotificationId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.Navigation("Notification");
        });

        modelBuilder.Entity("NotiHeart.Persistence.Notification", b =>
        {
            b.Navigation("Attachments");

            b.Navigation("Attempts");
        });
    }
}
