using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OroIdentityServers.EntityFramework.Entities;

[Table("ClientGrantTypes")]
public class ClientGrantTypeEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int ClientId { get; set; }

    [Required]
    [MaxLength(50)]
    public required string GrantType { get; set; }

    // Navigation property
    [ForeignKey("ClientId")]
    public virtual ClientEntity Client { get; set; } = null!;
}