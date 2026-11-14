namespace DocuMind.Application.Common.Interfaces;

/// <summary>
/// Provides access to the current date and time in UTC.
/// </summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
