using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NotiHeart.Data;

#nullable disable

namespace NotiHeart.Migrations;

[DbContext(typeof(NotificationDbContext))]
partial class NotificationDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasAnnotation("ProductVersion", "9.0.2")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);

        modelBuilder.Entity("NotiHeart.Models.Notification", b =>
            {
                b.Property<Guid>("Id")
                    .HasColumnType("uuid");

                b.Property<string>("Channel")
                    .IsRequired()
                    .HasColumnType("text");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("timestamp with time zone");

                b.Property<string>("LastError")
                    .HasColumnType("text");

                b.Property<string>("Recipient")
                    .IsRequired()
                    .HasColumnType("text");

                b.Property<string>("Status")
                    .IsRequired()
                    .HasColumnType("text");

                b.Property<string>("Text")
                    .IsRequired()
                    .HasColumnType("text");

                b.Property<DateTime>("UpdatedAt")
                    .HasColumnType("timestamp with time zone");

                b.HasKey("Id");

                b.HasIndex("CreatedAt");

                b.HasIndex("Status");

                b.ToTable("notifications");
            });

        modelBuilder.Entity("NotiHeart.Models.NotificationAttachment", b =>
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

        modelBuilder.Entity("NotiHeart.Models.NotificationAttempt", b =>
            {
                b.Property<Guid>("Id")
                    .HasColumnType("uuid");

                b.Property<int>("AttemptNo")
                    .HasColumnType("integer");

                b.Property<string>("Error")
                    .HasColumnType("text");

                b.Property<DateTime?>("FinishedAt")
                    .HasColumnType("timestamp with time zone");

                b.Property<Guid>("NotificationId")
                    .HasColumnType("uuid");

                b.Property<string>("Result")
                    .IsRequired()
                    .HasColumnType("text");

                b.Property<DateTime>("StartedAt")
                    .HasColumnType("timestamp with time zone");

                b.HasKey("Id");

                b.HasIndex("NotificationId");

                b.ToTable("notification_attempts");
            });

        modelBuilder.Entity("NotiHeart.Models.Notification", b =>
            {
                b.HasMany("NotiHeart.Models.NotificationAttachment", "Attachments")
                    .WithOne("Notification")
                    .HasForeignKey("NotificationId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.HasMany("NotiHeart.Models.NotificationAttempt", "Attempts")
                    .WithOne("Notification")
                    .HasForeignKey("NotificationId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.Navigation("Attachments");

                b.Navigation("Attempts");
            });
    }
}
