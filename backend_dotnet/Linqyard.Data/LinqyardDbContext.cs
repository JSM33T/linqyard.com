using Linqyard.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Data;
using System.Reflection.Emit;

namespace Linqyard.Data;

public class LinqyardDbContext : DbContext
{
    public LinqyardDbContext(DbContextOptions<LinqyardDbContext> options) : base(options)
    {
    }

    // DbSets for all entities
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<Tier> Tiers { get; set; }
    public DbSet<UserTier> UserTiers { get; set; }
    public DbSet<TierBillingCycle> TierBillingCycles { get; set; }
    public DbSet<Coupon> Coupons { get; set; }
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
    public DbSet<ViewTelemetry> ViewTelemetries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Enable PostgreSQL extensions
        modelBuilder.HasPostgresExtension("pgcrypto");
        modelBuilder.HasPostgresExtension("citext");

        ConfigureUserEntity(modelBuilder);
        ConfigureRoleEntity(modelBuilder);
        ConfigureUserRoleEntity(modelBuilder);
        ConfigureTierEntity(modelBuilder);
        ConfigureUserTierEntity(modelBuilder);
        ConfigureTierBillingCycleEntity(modelBuilder);
        ConfigureCouponEntity(modelBuilder);
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
        ConfigureViewTelemetryEntity(modelBuilder);

        SeedRoles(modelBuilder);
        SeedTiers(modelBuilder);
        SeedAppConfigs(modelBuilder);
        SeedTierBillingCycles(modelBuilder);
        SeedCoupons(modelBuilder);
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

        entity.HasMany(u => u.UserTiers)
              .WithOne(ut => ut.User)
              .HasForeignKey(ut => ut.UserId)
              .OnDelete(DeleteBehavior.Cascade);
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

    private void ConfigureTierEntity(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Tier>();

        // Primary key
        entity.HasKey(e => e.Id);

        entity.HasIndex(e => e.Name).IsUnique();

        entity.Property(e => e.Name)
              .HasColumnType("citext")
              .HasMaxLength(64);

        entity.Property(e => e.Currency)
              .HasMaxLength(3)
              .HasDefaultValue("INR");

        entity.HasMany(t => t.UserTiers)
              .WithOne(ut => ut.Tier)
              .HasForeignKey(ut => ut.TierId)
              .OnDelete(DeleteBehavior.Restrict);

        entity.HasMany(t => t.BillingCycles)
              .WithOne(bc => bc.Tier)
              .HasForeignKey(bc => bc.TierId)
              .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(t => t.Coupons)
              .WithOne(c => c.Tier)
              .HasForeignKey(c => c.TierId)
              .OnDelete(DeleteBehavior.SetNull);
    }

    private void ConfigureTierBillingCycleEntity(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<TierBillingCycle>();

        entity.HasIndex(e => new { e.TierId, e.BillingPeriod }).IsUnique();

        entity.Property(e => e.BillingPeriod)
              .HasColumnType("citext")
              .HasMaxLength(64);

        entity.Property(e => e.Description)
              .HasMaxLength(256);

        entity.Property(e => e.Amount)
              .HasDefaultValue(0);

        entity.Property(e => e.DurationMonths)
              .HasDefaultValue(1);

        entity.Property(e => e.IsActive)
              .HasDefaultValue(true);
    }

    private void ConfigureCouponEntity(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Coupon>();

        entity.HasIndex(e => e.Code).IsUnique();

        entity.Property(e => e.Code)
              .HasColumnType("citext")
              .HasMaxLength(64);

        entity.Property(e => e.Description)
              .HasMaxLength(256);

        entity.Property(e => e.DiscountPercentage)
              .HasPrecision(5, 2);

        entity.Property(e => e.CreatedAt)
              .HasDefaultValueSql("timezone('utc', now())");

        entity.Property(e => e.UpdatedAt)
              .HasDefaultValueSql("timezone('utc', now())");
    }

    private void ConfigureUserTierEntity(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<UserTier>();

        entity.HasKey(e => e.Id);

        entity.HasIndex(e => new { e.UserId, e.IsActive });
        entity.HasIndex(e => new { e.UserId, e.ActiveFrom });
        entity.HasIndex(e => new { e.UserId, e.ActiveUntil });
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

    private void ConfigureViewTelemetryEntity(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ViewTelemetry>();

        // Primary key
        entity.HasKey(e => e.Id);

        // Indexes for efficient querying
        entity.HasIndex(e => e.ProfileUserId);
        entity.HasIndex(e => e.ViewerUserId);
        entity.HasIndex(e => e.ViewedAt);
        entity.HasIndex(e => new { e.ProfileUserId, e.ViewedAt });
        entity.HasIndex(e => e.Source);
        entity.HasIndex(e => e.Fingerprint);
        entity.HasIndex(e => new { e.ProfileUserId, e.Source });

        // GIN index for JSONB UTM parameters
        entity.HasIndex(e => e.UtmParameters).HasMethod("gin");

        // Relationships
        entity.HasOne(vt => vt.ProfileUser)
            .WithMany()
            .HasForeignKey(vt => vt.ProfileUserId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(vt => vt.ViewerUser)
            .WithMany()
            .HasForeignKey(vt => vt.ViewerUserId)
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

    private void SeedTiers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tier>().HasData(
            new Tier { Id = 1, Name = "free", Currency = "INR", Description = "Free tier with basic features" },
            new Tier { Id = 2, Name = "plus", Currency = "INR", Description = "Plus tier with enhanced features" },
            new Tier { Id = 3, Name = "pro", Currency = "INR", Description = "Pro tier with premium features" }
        );
    }

    private void SeedTierBillingCycles(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TierBillingCycle>().HasData(
            new TierBillingCycle
            {
                Id = 1,
                TierId = 2,
                BillingPeriod = "monthly",
                Amount = 6900,
                DurationMonths = 1,
                Description = "Monthly subscription for Plus",
                IsActive = true
            },
            new TierBillingCycle
            {
                Id = 2,
                TierId = 2,
                BillingPeriod = "yearly",
                Amount = 70000,
                DurationMonths = 12,
                Description = "Yearly subscription for Plus",
                IsActive = true
            },
            new TierBillingCycle
            {
                Id = 3,
                TierId = 3,
                BillingPeriod = "monthly",
                Amount = 9900,
                DurationMonths = 1,
                Description = "Monthly subscription for Pro",
                IsActive = true
            },
            new TierBillingCycle
            {
                Id = 4,
                TierId = 3,
                BillingPeriod = "yearly",
                Amount = 95000,
                DurationMonths = 12,
                Description = "Yearly subscription for Pro",
                IsActive = true
            }
        );
    }

    private void SeedCoupons(ModelBuilder modelBuilder)
    {
        var now = new DateTimeOffset(2025, 10, 1, 0, 0, 0, TimeSpan.Zero);

        modelBuilder.Entity<Coupon>().HasData(
            new Coupon
            {
                Id = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                Code = "WELCOME10",
                Description = "Introductory 10% discount for Plus tier",
                DiscountPercentage = 10m,
                TierId = 2,
                MaxRedemptions = 500,
                RedemptionCount = 0,
                ValidFrom = now,
                ValidUntil = now.AddYears(1),
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            }
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
