using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Linqyard.Entities;

public class Tier
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = null!;

    [MaxLength(3)]
    [Column(TypeName = "varchar(3)")]
    public string Currency { get; set; } = "INR";

    public string? Description { get; set; }

    public ICollection<TierBillingCycle> BillingCycles { get; set; } = new List<TierBillingCycle>();

    public ICollection<UserTier> UserTiers { get; set; } = new List<UserTier>();

    public ICollection<Coupon> Coupons { get; set; } = new List<Coupon>();
}
