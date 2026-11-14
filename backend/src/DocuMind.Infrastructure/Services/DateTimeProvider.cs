using DocuMind.Application.Common.Interfaces;

namespace DocuMind.Infrastructure.Services;

/// <summary>
/// Production implementation returning current UTC system time.
/// </summary>
public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
