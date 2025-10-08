using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Linqyard.Entities;

public class RefreshToken
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid SessionId { get; set; }

    [Required]
    public string TokenHash { get; set; } = null!;

    [Column(TypeName = "timestamptz")]
    public DateTimeOffset IssuedAt { get; set; }

    [Column(TypeName = "timestamptz")]
    public DateTimeOffset ExpiresAt { get; set; }

    [Column(TypeName = "timestamptz")]
    public DateTimeOffset? RevokedAt { get; set; }

    public Guid? ReplacedById { get; set; }

    [Required]
    public Guid FamilyId { get; set; }

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(SessionId))]
    public Session Session { get; set; } = null!;

    [ForeignKey(nameof(ReplacedById))]
    public RefreshToken? ReplacedBy { get; set; }
}