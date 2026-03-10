namespace Shared.Domain.Common;

// Result<T> is a discriminated union: it's either a Success with a value, or a Failure with an Error.
// This is the backbone of our entire error-handling strategy.
//
// Instead of:
//   User? user = await GetUser(id);    // null means not found? or something else?
//   throw new UserNotFoundException(); // forces callers to know which exceptions to catch
//
// We write:
//   Result<User> result = await GetUser(id);
//   if (result.IsFailure) return result.Error;  // explicit, type-safe, no surprises
//
// Every command/query handler returns Result<T>. The API layer maps the Error.Type to HTTP status.
public class Result<T>
{
    public T? Value { get; }
    public Error Error { get; }
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    private Result(T value)
    {
        IsSuccess = true;
        Value = value;
        Error = Error.None;
    }

    private Result(Error error)
    {
        IsSuccess = false;
        Value = default;
        Error = error;
    }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(Error error) => new(error);

    // Implicit conversions let us return T or Error directly from methods that return Result<T>.
    // Example: return user;  instead of  return Result<User>.Success(user);
    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);
}

// Non-generic Result for commands that don't return a value (just success/failure).
public class Result
{
    public Error Error { get; }
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    protected Result(bool isSuccess, Error error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);

    public static implicit operator Result(Error error) => Failure(error);
}
