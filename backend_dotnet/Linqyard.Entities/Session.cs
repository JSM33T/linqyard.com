using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;

namespace Linqyard.Entities;

public class Session
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public string AuthMethod { get; set; } = null!; // EmailPassword | Google

    [Required]
    [Column(TypeName = "inet")]
    public IPAddress IpAddress { get; set; } = null!;

    [Required]
    public string UserAgent { get; set; } = null!;

    [Column(TypeName = "timestamptz")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column(TypeName = "timestamptz")]
    public DateTimeOffset LastSeenAt { get; set; }

    [Column(TypeName = "timestamptz")]
    public DateTimeOffset? RevokedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}