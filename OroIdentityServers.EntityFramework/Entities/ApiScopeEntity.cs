using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OroIdentityServers.EntityFramework.Entities;

[Table("ApiScopes")]
public class ApiScopeEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

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
    public virtual ICollection<ApiScopeClaimEntity> UserClaims { get; set; } = new List<ApiScopeClaimEntity>();
}