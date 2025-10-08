using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Linqyard.Entities;

public class AppConfig
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public string Key { get; set; } = null!;

    [Required]
    public string Value { get; set; } = null!;

    [Column(TypeName = "timestamptz")]
    public DateTimeOffset UpdatedAt { get; set; }
}