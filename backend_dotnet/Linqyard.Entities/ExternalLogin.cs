using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Linqyard.Entities;

public class ExternalLogin
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public string Provider { get; set; } = null!;

    [Required]
    public string ProviderUserId { get; set; } = null!;

    public string? ProviderEmail { get; set; }

    [Column(TypeName = "timestamptz")]
    public DateTimeOffset LinkedAt { get; set; }

    // Navigation property
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}