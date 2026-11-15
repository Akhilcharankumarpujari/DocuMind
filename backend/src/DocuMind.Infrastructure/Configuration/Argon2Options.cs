namespace DocuMind.Infrastructure.Configuration;

public class Argon2Options
{
    public const string SectionName = "Argon2";

    /// <summary>
    /// Memory size in KiB (default: 65536 KiB = 64 MB).
    /// </summary>
    public int MemorySize { get; set; } = 65536;

    /// <summary>
    /// Number of passes (iterations / time cost) (default: 3).
    /// </summary>
    public int Iterations { get; set; } = 3;

    /// <summary>
    /// Degree of parallelism (lanes / threads) (default: 1).
    /// </summary>
    public int DegreeOfParallelism { get; set; } = 1;

    /// <summary>
    /// Salt size in bytes (default: 16).
    /// </summary>
    public int SaltSize { get; set; } = 16;

    /// <summary>
    /// Hash length in bytes (default: 32).
    /// </summary>
    public int HashLength { get; set; } = 32;
}
