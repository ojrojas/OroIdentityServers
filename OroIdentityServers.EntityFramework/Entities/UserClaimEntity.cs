namespace OroIdentityServers.EntityFramework.Entities;

[Table("UserClaims")]
public class UserClaimEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    [MaxLength(250)]
    public required string Type { get; set; }

    [Required]
    [MaxLength(250)]
    public required string Value { get; set; }

    // Navigation property
    [ForeignKey("UserId")]
    public virtual UserEntity User { get; set; } = null!;
}