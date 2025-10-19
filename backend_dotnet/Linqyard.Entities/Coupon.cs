using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Linqyard.Entities;

public class Coupon
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(64)]
    [Column(TypeName = "citext")]
    public string Code { get; set; } = null!;

    [Column(TypeName = "numeric(5,2)")]
    public decimal DiscountPercentage { get; set; }

    [MaxLength(256)]
    public string? Description { get; set; }

    public int? TierId { get; set; }

    public int? MaxRedemptions { get; set; }

    public int RedemptionCount { get; set; }

    public DateTimeOffset? ValidFrom { get; set; }

    public DateTimeOffset? ValidUntil { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Tier? Tier { get; set; }
}
