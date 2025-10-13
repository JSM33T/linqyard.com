using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Linqyard.Entities;

/// <summary>
/// Tracks profile view telemetry including source, fingerprint, and location data
/// </summary>
public class ViewTelemetry
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// The user whose profile was viewed
    /// </summary>
    [Required]
    public Guid ProfileUserId { get; set; }

    /// <summary>
    /// The user who viewed the profile (if authenticated)
    /// </summary>
    public Guid? ViewerUserId { get; set; }

    /// <summary>
    /// Browser fingerprint to track unique visitors
    /// </summary>
    public string? Fingerprint { get; set; }

    /// <summary>
    /// Traffic source (e.g., 'direct', 'whatsapp', 'twitter', 'facebook', 'linkedin', 'instagram', 'google', 'other')
    /// </summary>
    [MaxLength(100)]
    public string? Source { get; set; }

    /// <summary>
    /// Referrer URL for more detailed source tracking
    /// </summary>
    [MaxLength(2048)]
    public string? Referrer { get; set; }

    /// <summary>
    /// UTM parameters for campaign tracking (stored as JSON)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? UtmParameters { get; set; }

    /// <summary>
    /// Geographic location - Latitude
    /// </summary>
    public double? Latitude { get; set; }

    /// <summary>
    /// Geographic location - Longitude
    /// </summary>
    public double? Longitude { get; set; }

    /// <summary>
    /// Location accuracy in meters
    /// </summary>
    public double? Accuracy { get; set; }

    /// <summary>
    /// City name (if available)
    /// </summary>
    [MaxLength(100)]
    public string? City { get; set; }

    /// <summary>
    /// Country code (ISO 3166-1 alpha-2)
    /// </summary>
    [MaxLength(2)]
    public string? Country { get; set; }

    /// <summary>
    /// Browser user agent string
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Device type (e.g., 'mobile', 'tablet', 'desktop')
    /// </summary>
    [MaxLength(50)]
    public string? DeviceType { get; set; }

    /// <summary>
    /// Operating system
    /// </summary>
    [MaxLength(100)]
    public string? Os { get; set; }

    /// <summary>
    /// Browser name
    /// </summary>
    [MaxLength(100)]
    public string? Browser { get; set; }

    /// <summary>
    /// IP address of the viewer
    /// </summary>
    [Column(TypeName = "inet")]
    public System.Net.IPAddress? IpAddress { get; set; }

    /// <summary>
    /// Session ID for grouping views in the same session
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Duration of the view in seconds (if tracked)
    /// </summary>
    public int? DurationSeconds { get; set; }

    /// <summary>
    /// Timestamp when the view occurred
    /// </summary>
    [Column(TypeName = "timestamptz")]
    public DateTimeOffset ViewedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(ProfileUserId))]
    public User ProfileUser { get; set; } = null!;

    [ForeignKey(nameof(ViewerUserId))]
    public User? ViewerUser { get; set; }
}
