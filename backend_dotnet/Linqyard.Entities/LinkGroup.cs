using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Linqyard.Entities;

public class LinkGroup
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [Column(TypeName = "citext")]
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    // Sequence used for ordering groups per user or globally
    public int Sequence { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    [Column(TypeName = "timestamptz")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column(TypeName = "timestamptz")]
    public DateTimeOffset UpdatedAt { get; set; }

    [Column(TypeName = "timestamptz")]
    public DateTimeOffset? DeletedAt { get; set; }

    // Navigation
    public ICollection<Link> Links { get; set; } = new List<Link>();

    // Owner
    public Guid? UserId { get; set; }
    public User? User { get; set; }
}
