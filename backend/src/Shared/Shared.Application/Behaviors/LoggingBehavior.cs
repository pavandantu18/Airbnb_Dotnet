using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Domain.Common;

namespace Shared.Application.Behaviors;

// Pipeline behaviors wrap every MediatR request like middleware wraps HTTP requests.
// Order in Program.cs determines execution order:
//   LoggingBehavior → ValidationBehavior → TransactionBehavior → Handler
//
// LoggingBehavior: logs every command/query with timing.
// In production this gives us full visibility into what the system is doing
// and how long each operation takes — without cluttering handler code.
public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        logger.LogInformation("Handling {RequestName}: {@Request}", requestName, request);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var response = await next(cancellationToken);
            stopwatch.Stop();

            // Log failures differently so they're easy to filter in Seq/Kibana
            if (response is Result result && result.IsFailure)
            {
                logger.LogWarning(
                    "Request {RequestName} failed in {ElapsedMs}ms. Error: {ErrorCode} - {ErrorDescription}",
                    requestName,
                    stopwatch.ElapsedMilliseconds,
                    result.Error.Code,
                    result.Error.Description);
            }
            else
            {
                logger.LogInformation(
                    "Handled {RequestName} in {ElapsedMs}ms",
                    requestName,
                    stopwatch.ElapsedMilliseconds);
            }

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(
                ex,
                "Request {RequestName} threw an exception after {ElapsedMs}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
