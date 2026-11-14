namespace DocuMind.Application.Common.Models;

/// <summary>
/// Represents the outcome of an operation without a return value.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }

    protected Result(bool isSuccess, string? error)
    {
        if (isSuccess && !string.IsNullOrWhiteSpace(error))
            throw new InvalidOperationException("A successful result cannot have an error.");

        if (!isSuccess && string.IsNullOrWhiteSpace(error))
            throw new InvalidOperationException("A failed result must specify an error message.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(string error) => new(false, error);
}

/// <summary>
/// Represents the outcome of an operation with a typed return value.
/// </summary>
/// <typeparam name="T">The type of the returned value.</typeparam>
public class Result<T> : Result
{
    private readonly T? _value;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Cannot access the value of a failed result. Error: {Error}");

    protected Result(bool isSuccess, T? value, string? error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    public static Result<T> Success(T value) => new(true, value, null);
    public new static Result<T> Failure(string error) => new(false, default, error);
}
