using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OroIdentityServers.EntityFramework.Entities;

[Table("Users")]
public class UserEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public required string TenantId { get; set; }

    [Required]
    [MaxLength(200)]
    public required string Username { get; set; }

    [Required]
    [MaxLength(500)]
    public required string PasswordHash { get; set; }

    [MaxLength(200)]
    public string? Email { get; set; }

    public bool EmailConfirmed { get; set; } = false;

    public bool Enabled { get; set; } = true;

    public DateTime Created { get; set; } = DateTime.UtcNow;

    public DateTime? LastModified { get; set; }

    public DateTime? LastLogin { get; set; }

    // Navigation properties
    public virtual ICollection<UserClaimEntity> Claims { get; set; } = new List<UserClaimEntity>();

    // Multi-tenancy
    public virtual TenantEntity? Tenant { get; set; }
}