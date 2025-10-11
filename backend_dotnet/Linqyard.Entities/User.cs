using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Linqyard.Entities;

public class User
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [Column(TypeName = "citext")]
    public string Email { get; set; } = null!;

    public bool EmailVerified { get; set; } = false;

    [Required]
    public string PasswordHash { get; set; } = null!;

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    // Profile fields (consolidated from separate Profile entity)
    [Required]
    [Column(TypeName = "citext")]
    public string Username { get; set; } = null!;

    public string? DisplayName { get; set; }

    public string? Bio { get; set; }

    public string? AvatarUrl { get; set; }

    public string? CoverUrl { get; set; }

    public string? Timezone { get; set; }

    public string? Locale { get; set; }

    public bool VerifiedBadge { get; set; } = false;

    public bool IsActive { get; set; } = true;

    // Tier relationship
    public int? TierId { get; set; }

    [Column(TypeName = "timestamptz")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column(TypeName = "timestamptz")]
    public DateTimeOffset UpdatedAt { get; set; }

    [Column(TypeName = "timestamptz")]
    public DateTimeOffset? DeletedAt { get; set; }

    // Navigation properties
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    [ForeignKey(nameof(TierId))]
    public Tier? Tier { get; set; }
    public ICollection<ExternalLogin> ExternalLogins { get; set; } = new List<ExternalLogin>();
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<TwoFactorMethod> TwoFactorMethods { get; set; } = new List<TwoFactorMethod>();
    public ICollection<TwoFactorCode> TwoFactorCodes { get; set; } = new List<TwoFactorCode>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public ICollection<Link> Links { get; set; } = new List<Link>();
    public ICollection<LinkGroup> LinkGroups { get; set; } = new List<LinkGroup>();
}

