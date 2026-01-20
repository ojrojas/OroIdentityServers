namespace OroIdentityServers.EntityFramework.Entities;

[Table("PersistedGrants")]
public class PersistedGrantEntity
{
    [Key]
    [MaxLength(200)]
    public required string Key { get; set; }

    [Required]
    [MaxLength(50)]
    public required string Type { get; set; }

    [Required]
    [MaxLength(200)]
    public required string SubjectId { get; set; }

    [Required]
    [MaxLength(200)]
    public required string ClientId { get; set; }

    public DateTime CreationTime { get; set; } = DateTime.UtcNow;

    public DateTime? Expiration { get; set; }

    [MaxLength(500)]
    public string? Data { get; set; }

    [MaxLength(100)]
    public string? SessionId { get; set; }

    [MaxLength(200)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(100)]
    public required string TenantId { get; set; }

    // Navigation property
    public virtual TenantEntity Tenant { get; set; } = null!;
}