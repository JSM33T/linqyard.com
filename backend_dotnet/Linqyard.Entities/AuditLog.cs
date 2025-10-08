using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;

namespace Linqyard.Entities;

public class AuditLog
{
    [Key]
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    [Required]
    public string Action { get; set; } = null!; // UserCreated, OtpRequested, OtpVerified, PasswordSet, LoginSuccess, LoginFailed, etc.

    public string? AuthMethod { get; set; }

    [Column(TypeName = "inet")]
    public IPAddress? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    [Column(TypeName = "jsonb")]
    public string? Metadata { get; set; } // JSON string for extra context

    [Column(TypeName = "timestamptz")]
    public DateTimeOffset At { get; set; }

    // Navigation property
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}