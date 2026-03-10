namespace Shared.Domain.Common;

// An Error is a structured description of what went wrong.
// Using a typed Error instead of throwing exceptions for business failures:
// - Exceptions are for truly exceptional cases (network down, disk full).
// - Business failures (invalid email, property not found) are expected — model them explicitly.
// - Makes error handling visible in method signatures instead of hidden try/catch chains.
//
// ErrorType lets the API layer map to the correct HTTP status without if/else chains.
public sealed record Error(string Code, string Description, ErrorType Type)
{
    // Predefined errors for cross-cutting concerns (every module can use these)
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);
    public static readonly Error NullValue = new("Error.NullValue", "A null value was provided.", ErrorType.Failure);
    public static readonly Error NotFound = new("Error.NotFound", "The requested resource was not found.", ErrorType.NotFound);
    public static readonly Error Unauthorized = new("Error.Unauthorized", "You are not authorized.", ErrorType.Unauthorized);
    public static readonly Error Forbidden = new("Error.Forbidden", "Access is forbidden.", ErrorType.Forbidden);
    public static readonly Error Conflict = new("Error.Conflict", "A conflict occurred.", ErrorType.Conflict);

    // Factory methods for quick error creation inside modules
    public static Error Failure(string code, string description) =>
        new(code, description, ErrorType.Failure);

    public static Error Validation(string code, string description) =>
        new(code, description, ErrorType.Validation);

    public static Error NotFoundError(string code, string description) =>
        new(code, description, ErrorType.NotFound);

    public static Error ConflictError(string code, string description) =>
        new(code, description, ErrorType.Conflict);
}

public enum ErrorType
{
    Failure,       // → HTTP 500
    Validation,    // → HTTP 400
    NotFound,      // → HTTP 404
    Conflict,      // → HTTP 409
    Unauthorized,  // → HTTP 401
    Forbidden      // → HTTP 403
}
