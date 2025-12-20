using Microsoft.EntityFrameworkCore;
using NotiHeart.Worker.Models;

namespace NotiHeart.Worker.Data;

public sealed class NotificationDbContext(DbContextOptions<NotificationDbContext> options) : DbContext(options)
{
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationAttempt> NotificationAttempts => Set<NotificationAttempt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("notifications");
            entity.HasKey(n => n.Id);
            entity.Property(n => n.Status).HasConversion<string>();
            entity.Property(n => n.UpdatedAt).IsRequired();
        });

        modelBuilder.Entity<NotificationAttempt>(entity =>
        {
            entity.ToTable("notification_attempts");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.AttemptNo).IsRequired();
            entity.Property(a => a.StartedAt).IsRequired();
            entity.Property(a => a.Result).IsRequired();
        });
    }
}
