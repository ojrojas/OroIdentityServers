namespace OroIdentityServers.EntityFramework.Entities;

[Table("Clients")]
public class ClientEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public required string TenantId { get; set; }

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
    public virtual ICollection<ClientGrantTypeEntity> AllowedGrantTypes { get; set; } = [];
    public virtual ICollection<ClientRedirectUriEntity> RedirectUris { get; set; } = [];
    public virtual ICollection<ClientScopeEntity> AllowedScopes { get; set; } = [];
    public virtual ICollection<ClientClaimEntity> Claims { get; set; } = [];

    // Multi-tenancy
    public virtual TenantEntity? Tenant { get; set; }
}