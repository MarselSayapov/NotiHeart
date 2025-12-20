using Microsoft.EntityFrameworkCore;
using NotiHeart.Contracts;

namespace NotiHeart.Persistence;

public sealed class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationAttempt> NotificationAttempts => Set<NotificationAttempt>();
    public DbSet<NotificationAttachment> NotificationAttachments => Set<NotificationAttachment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("uuid-ossp");

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("notifications");
            entity.HasKey(notification => notification.Id);
            entity.Property(notification => notification.Channel)
                .HasConversion<string>();
            entity.Property(notification => notification.Status)
                .HasConversion<string>();
            entity.Property(notification => notification.Metadata)
                .HasColumnType("jsonb");
            entity.Property(notification => notification.CreatedAt)
                .HasDefaultValueSql("now() at time zone 'utc'");
            entity.Property(notification => notification.UpdatedAt)
                .HasDefaultValueSql("now() at time zone 'utc'");
            entity.HasMany(notification => notification.Attempts)
                .WithOne(attempt => attempt.Notification)
                .HasForeignKey(attempt => attempt.NotificationId);
            entity.HasMany(notification => notification.Attachments)
                .WithOne(attachment => attachment.Notification)
                .HasForeignKey(attachment => attachment.NotificationId);
        });

        modelBuilder.Entity<NotificationAttempt>(entity =>
        {
            entity.ToTable("notification_attempts");
            entity.HasKey(attempt => attempt.Id);
            entity.Property(attempt => attempt.Status)
                .HasConversion<string>();
            entity.Property(attempt => attempt.Timestamp)
                .HasDefaultValueSql("now() at time zone 'utc'");
        });

        modelBuilder.Entity<NotificationAttachment>(entity =>
        {
            entity.ToTable("notification_attachments");
            entity.HasKey(attachment => attachment.Id);
        });
    }
}
