using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Linqyard.Entities;

public class UserTier
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public int TierId { get; set; }

    [Column(TypeName = "timestamptz")]
    public DateTimeOffset ActiveFrom { get; set; }

    [Column(TypeName = "timestamptz")]
    public DateTimeOffset? ActiveUntil { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Notes { get; set; }

    [Column(TypeName = "timestamptz")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column(TypeName = "timestamptz")]
    public DateTimeOffset UpdatedAt { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(TierId))]
    public Tier Tier { get; set; } = null!;
}
