using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OroIdentityServers.EntityFramework.Entities;

[Table("IdentityResources")]
public class IdentityResourceEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public required string TenantId { get; set; }

    [Required]
    [MaxLength(200)]
    public required string Name { get; set; }

    [MaxLength(200)]
    public string? DisplayName { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    public bool Enabled { get; set; } = true;

    public bool ShowInDiscoveryDocument { get; set; } = true;

    public DateTime Created { get; set; } = DateTime.UtcNow;

    public DateTime? LastModified { get; set; }

    // Navigation properties
    public virtual ICollection<IdentityResourceClaimEntity> UserClaims { get; set; } = new List<IdentityResourceClaimEntity>();

    // Multi-tenancy
    public virtual TenantEntity? Tenant { get; set; }
}