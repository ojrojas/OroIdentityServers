namespace OroIdentityServers.EntityFramework.Entities;

/// <summary>
/// Entity representing a stored event for event sourcing
/// </summary>
[Table("Events")]
public class EventEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    /// <summary>
    /// The aggregate ID this event belongs to
    /// </summary>
    [Required]
    [MaxLength(200)]
    public required string AggregateId { get; set; }

    /// <summary>
    /// The type of aggregate (e.g., "Client", "User", "Token")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public required string AggregateType { get; set; }

    /// <summary>
    /// The event type (e.g., "ClientCreated", "UserUpdated")
    /// </summary>
    [Required]
    [MaxLength(200)]
    public required string EventType { get; set; }

    /// <summary>
    /// The version of the aggregate when this event occurred
    /// </summary>
    [Required]
    public long Version { get; set; }

    /// <summary>
    /// The timestamp when the event occurred
    /// </summary>
    [Required]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Correlation ID for tracking related events
    /// </summary>
    [MaxLength(200)]
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Causation ID for event sourcing chains
    /// </summary>
    [MaxLength(200)]
    public string? CausationId { get; set; }

    /// <summary>
    /// The user who triggered the event
    /// </summary>
    [MaxLength(200)]
    public string? UserId { get; set; }

    /// <summary>
    /// The serialized event data
    /// </summary>
    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public required string Data { get; set; }

    /// <summary>
    /// Metadata about the event (optional)
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? Metadata { get; set; }

    /// <summary>
    /// Whether this event has been processed by all handlers
    /// </summary>
    public bool IsProcessed { get; set; } = false;

    /// <summary>
    /// When the event was processed
    /// </summary>
    public DateTime? ProcessedAt { get; set; }
}