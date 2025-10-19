using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Linqyard.Entities;

public class TierBillingCycle
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int TierId { get; set; }

    [Required]
    [MaxLength(64)]
    [Column(TypeName = "citext")]
    public string BillingPeriod { get; set; } = null!;

    [Required]
    public int Amount { get; set; }

    [Required]
    public int DurationMonths { get; set; }

    [MaxLength(256)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public Tier Tier { get; set; } = null!;
}
