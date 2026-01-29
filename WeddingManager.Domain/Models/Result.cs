namespace WeddingManager.Domain.Models;

public class Result
{
    private Result(bool isSuccess, IReadOnlyList<Error> errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    public bool IsSuccess { get; }
    public IReadOnlyList<Error> Errors { get; }

    public static Result Ok() => new(true, Array.Empty<Error>());

    public static Result Fail(Error error) => new(false, new[] { error });

    public static Result Fail(IEnumerable<Error> errors) =>
        new(false, errors as IReadOnlyList<Error> ?? errors.ToList());
}

public class Result<T>
{
    private Result(bool isSuccess, T? value, IReadOnlyList<Error> errors)
    {
        IsSuccess = isSuccess;
        Value = value;
        Errors = errors;
    }

    public bool IsSuccess { get; }
    public T? Value { get; }
    public IReadOnlyList<Error> Errors { get; }

    public static Result<T> Ok(T value) => new(true, value, Array.Empty<Error>());

    public static Result<T> Fail(Error error) => new(false, default, new[] { error });

    public static Result<T> Fail(IEnumerable<Error> errors) =>
        new(false, default, errors as IReadOnlyList<Error> ?? errors.ToList());
}
