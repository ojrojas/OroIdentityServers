namespace OroIdentityServers.EntityFramework.Entities;

[Table("IdentityResourceClaims")]
public class IdentityResourceClaimEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int IdentityResourceId { get; set; }

    [Required]
    [MaxLength(200)]
    public required string Type { get; set; }

    // Navigation property
    [ForeignKey("IdentityResourceId")]
    public virtual IdentityResourceEntity IdentityResource { get; set; } = null!;
}