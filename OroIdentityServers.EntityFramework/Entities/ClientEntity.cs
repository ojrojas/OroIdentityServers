using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OroIdentityServers.EntityFramework.Entities;

[Table("Clients")]
public class ClientEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public required string ClientId { get; set; }

    [Required]
    [MaxLength(200)]
    public required string ClientSecret { get; set; }

    [MaxLength(200)]
    public string? ClientName { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    public bool Enabled { get; set; } = true;

    public DateTime Created { get; set; } = DateTime.UtcNow;

    public DateTime? LastModified { get; set; }

    // Navigation properties
    public virtual ICollection<ClientGrantTypeEntity> AllowedGrantTypes { get; set; } = new List<ClientGrantTypeEntity>();
    public virtual ICollection<ClientRedirectUriEntity> RedirectUris { get; set; } = new List<ClientRedirectUriEntity>();
    public virtual ICollection<ClientScopeEntity> AllowedScopes { get; set; } = new List<ClientScopeEntity>();
    public virtual ICollection<ClientClaimEntity> Claims { get; set; } = new List<ClientClaimEntity>();
}