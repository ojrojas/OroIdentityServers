using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OroIdentityServers.EntityFramework.Entities;

[Table("ApiScopeClaims")]
public class ApiScopeClaimEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int ApiScopeId { get; set; }

    [Required]
    [MaxLength(200)]
    public required string Type { get; set; }

    // Navigation property
    [ForeignKey("ApiScopeId")]
    public virtual ApiScopeEntity ApiScope { get; set; } = null!;
}