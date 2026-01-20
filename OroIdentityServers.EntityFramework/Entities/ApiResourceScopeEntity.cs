namespace OroIdentityServers.EntityFramework.Entities;

[Table("ApiResourceScopes")]
public class ApiResourceScopeEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int ApiResourceId { get; set; }

    [Required]
    [MaxLength(200)]
    public required string Scope { get; set; }

    // Navigation property
    [ForeignKey("ApiResourceId")]
    public virtual ApiResourceEntity ApiResource { get; set; } = null!;
}