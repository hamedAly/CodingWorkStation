using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SemanticSearch.Application.Common.Exceptions;

namespace SemanticSearch.WebApi.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (NotFoundException ex)
        {
            _logger.LogInformation(ex, "Resource not found for request {Path}", context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status404NotFound, ex.Message);
        }
        catch (ConflictException ex)
        {
            _logger.LogInformation(ex, "Conflict for request {Path}", context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status409Conflict, ex.Message);
        }
        catch (PayloadTooLargeException ex)
        {
            _logger.LogInformation(ex, "Request too large for request {Path}", context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status413PayloadTooLarge, ex.Message);
        }
        catch (ServiceUnavailableException ex)
        {
            _logger.LogInformation(ex, "Service unavailable for request {Path}", context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status503ServiceUnavailable, ex.Message);
        }
        catch (ValidationException ex)
        {
            _logger.LogInformation(ex, "Validation failed for request {Path}", context.Request.Path);
            await WriteValidationProblemAsync(context, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for request {Path}", context.Request.Path);
            await WriteProblemAsync(context);
        }
    }

    private static async Task WriteValidationProblemAsync(HttpContext context, ValidationException ex)
    {
        var errorId = Guid.NewGuid().ToString("N");
        var errors = ex.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        var problem = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
            Detail = $"Reference errorId '{errorId}'.",
            Instance = context.Request.Path
        };
        problem.Extensions["errorId"] = errorId;

        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problem);
    }

    private static async Task WriteProblemAsync(HttpContext context, int statusCode, string detail)
    {
        var errorId = Guid.NewGuid().ToString("N");
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = statusCode == StatusCodes.Status500InternalServerError
                ? "An unexpected error occurred."
                : "The request could not be completed.",
            Detail = $"{detail} (errorId: {errorId})",
            Instance = context.Request.Path
        };
        problem.Extensions["errorId"] = errorId;

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problem);
    }

    private static Task WriteProblemAsync(HttpContext context)
        => WriteProblemAsync(context, StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
}
