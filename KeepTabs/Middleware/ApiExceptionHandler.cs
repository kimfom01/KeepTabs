using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace KeepTabs.Middleware;

/// <summary>
/// Maps known application failures to RFC 7807 problem responses.
/// </summary>
internal sealed class ApiExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ApiExceptionHandler> _logger;

    public ApiExceptionHandler(ILogger<ApiExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        switch (exception)
        {
            case ValidationException validationException:
                _logger.LogWarning(validationException, "Request validation failed for {Path}", httpContext.Request.Path);
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                await httpContext.Response.WriteAsJsonAsync(
                    new HttpValidationProblemDetails(validationException.Errors
                        .GroupBy(error => string.IsNullOrWhiteSpace(error.PropertyName) ? "request" : error.PropertyName)
                        .ToDictionary(
                            group => group.Key,
                            group => group.Select(error => error.ErrorMessage).ToArray()))
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Validation failed.",
                        Instance = httpContext.Request.Path
                    },
                    cancellationToken);
                return true;

            case KeyNotFoundException notFoundException:
                _logger.LogWarning(notFoundException, "Resource was not found for {Path}", httpContext.Request.Path);
                return await WriteProblemAsync(
                    httpContext,
                    StatusCodes.Status404NotFound,
                    "Not found.",
                    "The requested resource was not found.",
                    cancellationToken);

            case UnauthorizedAccessException unauthorizedException:
                _logger.LogWarning(unauthorizedException, "Unauthorized request for {Path}", httpContext.Request.Path);
                return await WriteProblemAsync(
                    httpContext,
                    StatusCodes.Status401Unauthorized,
                    "Unauthorized.",
                    "Authentication is required.",
                    cancellationToken);

            default:
                return false;
        }
    }

    private static async Task<bool> WriteProblemAsync(
        HttpContext httpContext,
        int statusCode,
        string title,
        string detail,
        CancellationToken cancellationToken)
    {
        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Instance = httpContext.Request.Path
            },
            cancellationToken);

        return true;
    }
}
