using System.ComponentModel.DataAnnotations;

namespace Linqyard.Entities;

public class Tier
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    // Navigation property - user tier assignments for this tier
    public ICollection<UserTier> UserTiers { get; set; } = new List<UserTier>();
}
