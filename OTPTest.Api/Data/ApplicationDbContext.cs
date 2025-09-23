using Microsoft.EntityFrameworkCore;
using OTPTest.Api.Models;

namespace OTPTest.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<OtpEntry> OtpEntries => Set<OtpEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<OtpEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.Email, e.Status });
            entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
            entity.Property(e => e.CodeHash).HasMaxLength(128).IsRequired();
            entity.Property(e => e.Salt).HasMaxLength(64).IsRequired();
            entity.Property(e => e.MaxAttempts).HasDefaultValue(10);
        });
    }
}

