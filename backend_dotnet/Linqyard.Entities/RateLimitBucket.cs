using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Linqyard.Entities;

public class RateLimitBucket
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(256)]
    public string Key { get; set; } = null!; // e.g., "otp:email@example.com", "login:203.0.113.5"

    [Column(TypeName = "timestamptz")]
    public DateTimeOffset WindowStart { get; set; }

    public int Count { get; set; }
}
