using System.ComponentModel.DataAnnotations;

namespace Linqyard.Entities;

public class Tier
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    // Navigation property - users who have this tier
    public ICollection<User> Users { get; set; } = new List<User>();
}
