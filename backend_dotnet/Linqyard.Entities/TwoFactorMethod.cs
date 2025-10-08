using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Linqyard.Entities;

public class TwoFactorMethod
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public string Type { get; set; } = null!; // TOTP | SMS | Email | BackupCodes

    public string? Secret { get; set; } // TOTP secret

    public string? PhoneNumber { get; set; } // SMS

    public bool IsActive { get; set; }

    [Column(TypeName = "timestamptz")]
    public DateTimeOffset CreatedAt { get; set; }

    // Navigation property
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    public ICollection<TwoFactorCode> TwoFactorCodes { get; set; } = new List<TwoFactorCode>();
}