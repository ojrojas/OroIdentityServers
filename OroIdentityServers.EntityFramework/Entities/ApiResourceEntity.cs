using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OroIdentityServers.EntityFramework.Entities;

[Table("ApiResources")]
public class ApiResourceEntity
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

    public DateTime Created { get; set; } = DateTime.UtcNow;

    public DateTime? LastModified { get; set; }

    // Navigation properties
    public virtual ICollection<ApiResourceClaimEntity> UserClaims { get; set; } = new List<ApiResourceClaimEntity>();
    public virtual ICollection<ApiResourceScopeEntity> Scopes { get; set; } = new List<ApiResourceScopeEntity>();
    public virtual ICollection<ApiResourceSecretEntity> Secrets { get; set; } = new List<ApiResourceSecretEntity>();
}