namespace DocuMind.Domain.Exceptions;

/// <summary>
/// Exception thrown when a requested domain entity cannot be located.
/// </summary>
public class EntityNotFoundException : DomainException
{
    public string EntityName { get; }
    public object Key { get; }

    public EntityNotFoundException(string entityName, object key)
        : base($"Entity '{entityName}' with identifier '{key}' was not found.")
    {
        EntityName = entityName;
        Key = key;
    }
}
