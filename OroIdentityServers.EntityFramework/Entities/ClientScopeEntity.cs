namespace OroIdentityServers.EntityFramework.Entities;

[Table("ClientScopes")]
public class ClientScopeEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int ClientId { get; set; }

    [Required]
    [MaxLength(200)]
    public required string Scope { get; set; }

    // Navigation property
    [ForeignKey("ClientId")]
    public virtual ClientEntity Client { get; set; } = null!;
}