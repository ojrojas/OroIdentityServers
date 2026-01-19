namespace OroIdentityServers.EntityFramework.Entities;

/// <summary>
/// Entity representing a tenant in a multi-tenant system
/// </summary>
[Table("Tenants")]
public class TenantEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Unique identifier for the tenant
    /// </summary>
    [Required]
    [MaxLength(100)]
    public required string TenantId { get; set; }

    /// <summary>
    /// Display name for the tenant
    /// </summary>
    [Required]
    [MaxLength(200)]
    public required string Name { get; set; }

    /// <summary>
    /// Description of the tenant
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Domain name associated with this tenant
    /// </summary>
    [MaxLength(200)]
    public string? Domain { get; set; }

    /// <summary>
    /// Connection string for tenant-specific database (optional)
    /// </summary>
    [MaxLength(2000)]
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Whether this tenant is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Whether this tenant has its own database
    /// </summary>
    public bool IsIsolated { get; set; } = false;

    /// <summary>
    /// Tenant-specific configuration (JSON)
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? Configuration { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime Created { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last modification timestamp
    /// </summary>
    public DateTime? LastModified { get; set; }

    // Navigation properties
    public virtual ICollection<ClientEntity> Clients { get; set; } = [];
    public virtual ICollection<UserEntity> Users { get; set; } = [];
    public virtual ICollection<ApiResourceEntity> ApiResources { get; set; } = [];
    public virtual ICollection<IdentityResourceEntity> IdentityResources { get; set; } = [];
    public virtual ICollection<PersistedGrantEntity> PersistedGrants { get; set; } = [];
}