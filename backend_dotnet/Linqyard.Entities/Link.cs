using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Linqyard.Entities;

public class Link
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [Column(TypeName = "citext")]
    public string Name { get; set; } = null!;

    [Required]
    [Column(TypeName = "citext")]
    public string Url { get; set; } = null!;

    public string? Description { get; set; }

    [Column(TypeName = "text[]")]
    public List<string>? Tags { get; set; }
    // Sequence used for ordering links within a group or per user
    public int Sequence { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    [Column(TypeName = "timestamptz")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column(TypeName = "timestamptz")]
    public DateTimeOffset UpdatedAt { get; set; }

    [Column(TypeName = "timestamptz")]
    public DateTimeOffset? DeletedAt { get; set; }

    // Foreign keys
    public Guid? UserId { get; set; }
    public User? User { get; set; }

    public Guid? GroupId { get; set; }
    public LinkGroup? Group { get; set; }
}
