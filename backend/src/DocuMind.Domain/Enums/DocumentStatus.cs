namespace DocuMind.Domain.Enums;

/// <summary>
/// Processing lifecycle state of an uploaded document.
/// </summary>
public enum DocumentStatus
{
    Pending = 0,
    Processing = 1,
    Ready = 2,
    Failed = 3
}
