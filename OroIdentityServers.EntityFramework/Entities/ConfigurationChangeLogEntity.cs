using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OroIdentityServers.EntityFramework.Entities;

[Table("ConfigurationChangeLogs")]
public class ConfigurationChangeLogEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required]
    [MaxLength(100)]
    public required string EntityType { get; set; } // "Client", "User", "ApiResource", etc.

    [Required]
    [MaxLength(200)]
    public required string EntityId { get; set; } // ClientId, UserId, etc.

    [Required]
    [MaxLength(50)]
    public required string ChangeType { get; set; } // "Created", "Updated", "Deleted"

    [Required]
    public DateTime ChangeTime { get; set; } = DateTime.UtcNow;

    [MaxLength(200)]
    public string? ChangedBy { get; set; } // User who made the change

    [MaxLength(1000)]
    public string? ChangeDescription { get; set; }

    [Column(TypeName = "jsonb")]
    public string? OldValues { get; set; } // JSON of old values

    [Column(TypeName = "jsonb")]
    public string? NewValues { get; set; } // JSON of new values
}