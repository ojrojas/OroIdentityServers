namespace OroIdentityServers.EntityFramework.Entities;

[Table("ApiResourceSecrets")]
public class ApiResourceSecretEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int ApiResourceId { get; set; }

    [Required]
    [MaxLength(1000)]
    public required string Value { get; set; }

    [MaxLength(250)]
    public string? Type { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    public DateTime? Expiration { get; set; }

    // Navigation property
    [ForeignKey("ApiResourceId")]
    public virtual ApiResourceEntity ApiResource { get; set; } = null!;
}