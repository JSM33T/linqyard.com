using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Linqyard.Entities;

public class UserRole
{
    [Key]
    [Column(Order = 0)]
    public Guid UserId { get; set; }

    [Key]
    [Column(Order = 1)]
    public int RoleId { get; set; }

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(RoleId))]
    public Role Role { get; set; } = null!;
}