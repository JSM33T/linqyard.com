using Microsoft.EntityFrameworkCore;
using Linqyard.Entities;
using System.Net;

namespace Linqyard.Api.Data;

public class LinqyardDbContext : DbContext
{
    public LinqyardDbContext(DbContextOptions<LinqyardDbContext> options) : base(options)
    {
    }

    // DbSets for all entities
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<ExternalLogin> ExternalLogins { get; set; }
    public DbSet<OtpCode> OtpCodes { get; set; }
    public DbSet<Session> Sessions { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<TwoFactorMethod> TwoFactorMethods { get; set; }
    public DbSet<TwoFactorCode> TwoFactorCodes { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<RateLimitBucket> RateLimitBuckets { get; set; }
    public DbSet<Link> Links { get; set; }
    public DbSet<LinkGroup> LinkGroups { get; set; }
    public DbSet<Analytics> Analytics { get; set; }
    public DbSet<AppConfig> AppConfigs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Enable PostgreSQL extensions
        modelBuilder.HasPostgresExtension("pgcrypto");
        modelBuilder.HasPostgresExtension("citext");

        ConfigureUserEntity(modelBuilder);
        ConfigureRoleEntity(modelBuilder);
        ConfigureUserRoleEntity(modelBuilder);
        ConfigureExternalLoginEntity(modelBuilder);
        ConfigureOtpCodeEntity(modelBuilder);
        ConfigureSessionEntity(modelBuilder);
        ConfigureRefreshTokenEntity(modelBuilder);
        ConfigureTwoFactorMethodEntity(modelBuilder);
        ConfigureTwoFactorCodeEntity(modelBuilder);
        ConfigureAuditLogEntity(modelBuilder);
    ConfigureAnalyticsEntity(modelBuilder);
        ConfigureRateLimitBucketEntity(modelBuilder);
    ConfigureLinkEntity(modelBuilder);
    ConfigureLinkGroupEntity(modelBuilder);
        ConfigureAppConfigEntity(modelBuilder);

        SeedRoles(modelBuilder);
        SeedAppConfigs(modelBuilder);
    }

    private void ConfigureUserEntity(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<User>();

        // Primary key
        entity.HasKey(e => e.Id);

        // Unique constraints
        entity.HasIndex(e => e.Email).IsUnique();
        entity.HasIndex(e => e.Username).IsUnique(); // Username is now part of User entity

        // Global query filter for soft delete
        // Note: Commented out to avoid conflicts with required navigation properties
        // You can implement soft delete logic in your services instead
        // entity.HasQueryFilter(e => e.DeletedAt == null);

        entity.HasMany(u => u.UserRoles)
              .WithOne(ur => ur.User)
              .HasForeignKey(ur => ur.UserId)
              .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(u => u.ExternalLogins)
              .WithOne(el => el.User)
              .HasForeignKey(el => el.UserId)
              .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(u => u.Sessions)
              .WithOne(s => s.User)
              .HasForeignKey(s => s.UserId)
              .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(u => u.RefreshTokens)
              .WithOne(rt => rt.User)
              .HasForeignKey(rt => rt.UserId)
              .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(u => u.TwoFactorMethods)
              .WithOne(tfm => tfm.User)
              .HasForeignKey(tfm => tfm.UserId)
              .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(u => u.TwoFactorCodes)
              .WithOne(tfc => tfc.User)
              .HasForeignKey(tfc => tfc.UserId)
              .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(u => u.AuditLogs)
              .WithOne(al => al.User)
              .HasForeignKey(al => al.UserId)
              .OnDelete(DeleteBehavior.SetNull);
    }

    private void ConfigureRoleEntity(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Role>();

        // Primary key
        entity.HasKey(e => e.Id);

        // Unique constraints
        entity.HasIndex(e => e.Name).IsUnique();

        // Relationships
        entity.HasMany(r => r.UserRoles)
              .WithOne(ur => ur.Role)
              .HasForeignKey(ur => ur.RoleId)
              .OnDelete(DeleteBehavior.Cascade);
    }

    private void ConfigureUserRoleEntity(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<UserRole>();

        // Composite primary key
        entity.HasKey(e => new { e.UserId, e.RoleId });
    }

    private void ConfigureExternalLoginEntity(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ExternalLogin>();

        // Primary key
        entity.HasKey(e => e.Id);

        // Unique constraints
        entity.HasIndex(e => new { e.Provider, e.ProviderUserId }).IsUnique();
    }

    private void ConfigureOtpCodeEntity(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<OtpCode>();

        // Primary key
        entity.HasKey(e => e.Id);

        // Indexes
        entity.HasIndex(e => new { e.Email, e.Purpose, e.ExpiresAt });
    }

    private void ConfigureSessionEntity(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Session>();

        // Primary key
        entity.HasKey(e => e.Id);

        // Indexes
        entity.HasIndex(e => new { e.UserId, e.LastSeenAt });

        // Relationships
        entity.HasMany(s => s.RefreshTokens)
              .WithOne(rt => rt.Session)
              .HasForeignKey(rt => rt.SessionId)
              .OnDelete(DeleteBehavior.Cascade);
    }

    private void ConfigureRefreshTokenEntity(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<RefreshToken>();

        // Primary key
        entity.HasKey(e => e.Id);

        // Indexes
        entity.HasIndex(e => new { e.UserId, e.SessionId });
        entity.HasIndex(e => e.FamilyId);

        // Self-referencing relationship
        entity.HasOne(rt => rt.ReplacedBy)
              .WithMany()
              .HasForeignKey(rt => rt.ReplacedById)
              .OnDelete(DeleteBehavior.SetNull);
    }

    private void ConfigureTwoFactorMethodEntity(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<TwoFactorMethod>();

        // Primary key
        entity.HasKey(e => e.Id);

        // Relationships
        entity.HasMany(tfm => tfm.TwoFactorCodes)
              .WithOne(tfc => tfc.Method)
              .HasForeignKey(tfc => tfc.MethodId)
              .OnDelete(DeleteBehavior.Cascade);
    }

    private void ConfigureTwoFactorCodeEntity(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<TwoFactorCode>();

        // Primary key
        entity.HasKey(e => e.Id);

        // Indexes
        entity.HasIndex(e => new { e.UserId, e.ExpiresAt });
    }

    private void ConfigureAuditLogEntity(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<AuditLog>();

        // Primary key
        entity.HasKey(e => e.Id);

        // Indexes
        entity.HasIndex(e => new { e.UserId, e.At });
        entity.HasIndex(e => e.Metadata).HasMethod("gin"); // GIN index for JSONB
    }

    private void ConfigureRateLimitBucketEntity(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<RateLimitBucket>();

        // Primary key
        entity.HasKey(e => e.Id);

        // Indexes
        entity.HasIndex(e => new { e.Key, e.WindowStart });
    }

    private void ConfigureAnalyticsEntity(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Analytics>();

        entity.HasKey(a => a.Id);
        entity.HasIndex(a => a.LinkId);
        entity.HasIndex(a => a.UserId);
        entity.HasIndex(a => a.At);
    }

    private void ConfigureAppConfigEntity(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<AppConfig>();

        // Primary key
        entity.HasKey(e => e.Id);

        // Unique constraints
        entity.HasIndex(e => e.Key).IsUnique();
    }

    private void ConfigureLinkGroupEntity(ModelBuilder modelBuilder)
    {
      var entity = modelBuilder.Entity<LinkGroup>();

      // Primary key
      entity.HasKey(e => e.Id);

      // Unique or searchable indexes
      entity.HasIndex(e => e.Name);

    // Index to support ordering of groups per user
    entity.HasIndex(e => new { e.UserId, e.Sequence });

    // Relationship to owner user
    entity.HasOne(lg => lg.User)
        .WithMany(u => u.LinkGroups)
        .HasForeignKey(lg => lg.UserId)
        .OnDelete(DeleteBehavior.SetNull);

      // Relationships
      entity.HasMany(lg => lg.Links)
          .WithOne(l => l.Group)
          .HasForeignKey(l => l.GroupId)
          .OnDelete(DeleteBehavior.SetNull);
    }

    private void ConfigureLinkEntity(ModelBuilder modelBuilder)
    {
      var entity = modelBuilder.Entity<Link>();

      // Primary key
      entity.HasKey(e => e.Id);

      // Indexes
        entity.HasIndex(e => new { e.UserId });
        entity.HasIndex(e => new { e.GroupId });
        // Composite indexes to support ordering by Sequence within user/group
        entity.HasIndex(e => new { e.UserId, e.Sequence });
        entity.HasIndex(e => new { e.GroupId, e.Sequence });

            // Relationships
      entity.HasOne(l => l.User)
          .WithMany(u => u.Links)
          .HasForeignKey(l => l.UserId)
          .OnDelete(DeleteBehavior.SetNull);

      entity.HasOne(l => l.Group)
          .WithMany(g => g.Links)
          .HasForeignKey(l => l.GroupId)
          .OnDelete(DeleteBehavior.SetNull);
    }

    private void SeedRoles(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Name = "admin", Description = "Administrator with full system access" },
            new Role { Id = 2, Name = "mod", Description = "Moderator with moderation privileges" },
            new Role { Id = 3, Name = "user", Description = "Standard user" }
        );
    }

    private void SeedAppConfigs(ModelBuilder modelBuilder)
    {
        // Use static values instead of dynamic ones to avoid model changes
        var baseDate = new DateTimeOffset(2025, 9, 20, 0, 0, 0, TimeSpan.Zero);
        
        modelBuilder.Entity<AppConfig>().HasData(
            new AppConfig { Id = new Guid("11111111-1111-1111-1111-111111111111"), Key = "GoogleLoginEnabled", Value = "true", UpdatedAt = baseDate },
            new AppConfig { Id = new Guid("22222222-2222-2222-2222-222222222222"), Key = "OtpExpiryMinutes", Value = "10", UpdatedAt = baseDate },
            new AppConfig { Id = new Guid("33333333-3333-3333-3333-333333333333"), Key = "OtpMaxAttempts", Value = "5", UpdatedAt = baseDate },
            new AppConfig { Id = new Guid("44444444-4444-4444-4444-444444444444"), Key = "SessionIdleTimeoutDays", Value = "14", UpdatedAt = baseDate },
            new AppConfig { Id = new Guid("55555555-5555-5555-5555-555555555555"), Key = "SessionAbsoluteLifetimeDays", Value = "60", UpdatedAt = baseDate },
            new AppConfig { Id = new Guid("66666666-6666-6666-6666-666666666666"), Key = "TwoFactorRequired", Value = "false", UpdatedAt = baseDate },
            new AppConfig { Id = new Guid("77777777-7777-7777-7777-777777777777"), Key = "SignupDisabled", Value = "false", UpdatedAt = baseDate },
            new AppConfig { Id = new Guid("88888888-8888-8888-8888-888888888888"), Key = "PasswordMinLength", Value = "8", UpdatedAt = baseDate }
        );
    }
}
