namespace OroIdentityServers.EntityFramework.Entities;

[Table("ClientClaims")]
public class ClientClaimEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int ClientId { get; set; }

    [Required]
    [MaxLength(250)]
    public required string Type { get; set; }

    [Required]
    [MaxLength(250)]
    public required string Value { get; set; }

    // Navigation property
    [ForeignKey("ClientId")]
    public virtual ClientEntity Client { get; set; } = null!;
}