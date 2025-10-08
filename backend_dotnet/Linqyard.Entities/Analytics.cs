using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Linqyard.Entities;

public class Analytics
{
    [Key]
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public Guid LinkId { get; set; }

    public string? Fingerprint { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? Accuracy { get; set; }

    public string? UserAgent { get; set; }

    [Column(TypeName = "inet")]
    public System.Net.IPAddress? IpAddress { get; set; }

    [Column(TypeName = "timestamptz")]
    public DateTimeOffset At { get; set; }

    // Navigation
    public User? User { get; set; }
}
