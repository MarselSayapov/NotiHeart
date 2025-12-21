using Microsoft.EntityFrameworkCore;
using NotiHeart.Models;

namespace NotiHeart.Data;

public sealed class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options)
    {
        Database.EnsureCreated();
    }

    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationAttachment> NotificationAttachments => Set<NotificationAttachment>();
    public DbSet<NotificationAttempt> NotificationAttempts => Set<NotificationAttempt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("notifications");
            entity.HasKey(n => n.Id);
            entity.Property(n => n.Channel).HasConversion<string>();
            entity.Property(n => n.Status).HasConversion<string>();
            entity.Property(n => n.Recipient).IsRequired();
            entity.Property(n => n.Text).IsRequired();
            entity.Property(n => n.CreatedAt).IsRequired();
            entity.Property(n => n.UpdatedAt).IsRequired();
            entity.HasIndex(n => n.Status);
            entity.HasIndex(n => n.CreatedAt);
        });

        modelBuilder.Entity<NotificationAttachment>(entity =>
        {
            entity.ToTable("notification_attachments");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.FileName).IsRequired();
            entity.Property(a => a.ContentType).IsRequired();
            entity.Property(a => a.Content).IsRequired();
            entity.Property(a => a.Size).IsRequired();
            entity.HasIndex(a => a.NotificationId);
        });

        modelBuilder.Entity<NotificationAttempt>(entity =>
        {
            entity.ToTable("notification_attempts");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.AttemptNo).IsRequired();
            entity.Property(a => a.StartedAt).IsRequired();
            entity.Property(a => a.Result).IsRequired();
            entity.HasIndex(a => a.NotificationId);
        });

        modelBuilder.Entity<Notification>()
            .HasMany(n => n.Attachments)
            .WithOne(a => a.Notification!)
            .HasForeignKey(a => a.NotificationId);

        modelBuilder.Entity<Notification>()
            .HasMany(n => n.Attempts)
            .WithOne(a => a.Notification!)
            .HasForeignKey(a => a.NotificationId);
    }
}
