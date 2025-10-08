using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Linqyard.Entities;

public class TwoFactorCode
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid MethodId { get; set; }

    [Required]
    public string CodeHash { get; set; } = null!;

    [Column(TypeName = "timestamptz")]
    public DateTimeOffset ExpiresAt { get; set; }

    [Column(TypeName = "timestamptz")]
    public DateTimeOffset? ConsumedAt { get; set; }

    [Required]
    public string Purpose { get; set; } = null!; // Login | Recovery

    [Column(TypeName = "timestamptz")]
    public DateTimeOffset CreatedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(MethodId))]
    public TwoFactorMethod Method { get; set; } = null!;
}