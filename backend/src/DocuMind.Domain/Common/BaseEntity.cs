namespace DocuMind.Domain.Common;

/// <summary>
/// Base entity containing the primary identity.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
}
