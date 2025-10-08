using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Linqyard.Entities;

public class OtpCode
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [Column(TypeName = "citext")]
    public string Email { get; set; } = null!;

    [Required]
    public string CodeHash { get; set; } = null!;

    [Required]
    public string Purpose { get; set; } = null!; // Signup | PasswordReset | EmailChange

    [Column(TypeName = "timestamptz")]
    public DateTimeOffset ExpiresAt { get; set; }

    [Column(TypeName = "timestamptz")]
    public DateTimeOffset? ConsumedAt { get; set; }

    public int Attempts { get; set; } = 0;
}