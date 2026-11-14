namespace DocuMind.Domain.Common;

/// <summary>
/// Contract for entities that track creation and update timestamps in UTC.
/// </summary>
public interface IAuditableEntity
{
    DateTime CreatedAtUtc { get; set; }
    DateTime? UpdatedAtUtc { get; set; }
}
