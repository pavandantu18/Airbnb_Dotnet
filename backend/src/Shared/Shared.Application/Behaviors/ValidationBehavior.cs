using FluentValidation;
using MediatR;
using Shared.Domain.Common;

namespace Shared.Application.Behaviors;

// ValidationBehavior runs FluentValidation validators before any handler executes.
// This means:
//   - Handlers can assume their input is already valid — no defensive checks inside handlers.
//   - Validation rules live in one place (the Validator class), not scattered across the handler.
//   - Invalid requests never hit the database.
//
// How it works:
//   1. MediatR resolves all IValidator<TRequest> registered in DI for this request type.
//   2. Runs all validators in parallel.
//   3. If any fail, returns a Validation error immediately — handler never runs.
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : class
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next(cancellationToken);

        // Run all validators concurrently
        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(request, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
            return await next(cancellationToken);

        // Combine all validation errors into one structured error.
        // The first error's property name becomes the code for easy identification.
        var errorMessage = string.Join("; ", failures.Select(f => f.ErrorMessage));
        var errorCode = $"Validation.{failures[0].PropertyName}";

        // We need to return TResponse which is Result<T> — use reflection-free approach
        // by creating the error and letting the implicit conversion operator do the work.
        var error = Error.Validation(errorCode, errorMessage);

        // TResponse must be a Result type — this is enforced by only registering
        // this behavior for ICommand and IQuery which always return Result<T>
        return CreateFailureResult(error);
    }

    private static TResponse CreateFailureResult(Error error)
    {
        // Use the static factory pattern on Result types
        if (typeof(TResponse) == typeof(Result))
            return (Result.Failure(error) as TResponse)!;

        // For Result<T>, invoke Result<T>.Failure(error) via reflection
        var resultType = typeof(TResponse).GetGenericArguments()[0];
        var failureMethod = typeof(Result<>)
            .MakeGenericType(resultType)
            .GetMethod(nameof(Result<object>.Failure))!;

        return (TResponse)failureMethod.Invoke(null, [error])!;
    }
}
