namespace OroIdentityServers.EntityFramework.Entities;

[Table("ClientRedirectUris")]
public class ClientRedirectUriEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int ClientId { get; set; }

    [Required]
    [MaxLength(2000)]
    public required string RedirectUri { get; set; }

    // Navigation property
    [ForeignKey("ClientId")]
    public virtual ClientEntity Client { get; set; } = null!;
}