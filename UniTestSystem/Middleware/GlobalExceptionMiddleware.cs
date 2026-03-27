using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Net.Http.Headers;

namespace UniTestSystem.Middleware;

public sealed class GlobalExceptionMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    private const string ErrorPathPrefix = "/Home/Error";

    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var isDevelopment = _environment.IsDevelopment();
        var responseModel = CreateErrorResponse(exception, isDevelopment);

        _logger.LogError(
            exception,
            "Unhandled exception at {Method} {Path}. TraceId: {TraceId}",
            context.Request.Method,
            context.Request.Path,
            context.TraceIdentifier);

        if (context.Response.HasStarted)
        {
            context.Abort();
            return;
        }

        if (IsApiRequest(context))
        {
            context.Response.Clear();
            context.Response.StatusCode = responseModel.StatusCode;
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsync(JsonSerializer.Serialize(responseModel.Payload, JsonOptions));
            return;
        }

        if (isDevelopment)
        {
            context.Response.Clear();
            context.Response.StatusCode = responseModel.StatusCode;
            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync(exception.ToString());
            return;
        }

        if (context.Request.Path.StartsWithSegments(ErrorPathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync("Internal server error");
            return;
        }

        context.Response.Clear();
        context.Response.Redirect($"{ErrorPathPrefix}/{responseModel.StatusCode}");
    }

    public static bool IsApiRequest(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (context.Request.Path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (context.GetEndpoint()?.Metadata.GetMetadata<Microsoft.AspNetCore.Mvc.ApiControllerAttribute>() is not null)
        {
            return true;
        }

        if (context.Request.Headers.TryGetValue(HeaderNames.Accept, out var acceptHeader)
            && acceptHeader.Any(v => !string.IsNullOrWhiteSpace(v)
                                     && v.Contains("application/json", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return context.Request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static ErrorResponse CreateErrorResponse(Exception exception, bool isDevelopment)
    {
        return exception switch
        {
            UnauthorizedAccessException => new ErrorResponse(
                StatusCodes.Status401Unauthorized,
                new
                {
                    error = "Unauthorized",
                    detail = isDevelopment ? exception.ToString() : null
                }),

            KeyNotFoundException => new ErrorResponse(
                StatusCodes.Status404NotFound,
                new
                {
                    error = "Resource not found",
                    detail = isDevelopment ? exception.ToString() : null
                }),

            InvalidOperationException => new ErrorResponse(
                StatusCodes.Status400BadRequest,
                new
                {
                    error = exception.Message,
                    detail = isDevelopment ? exception.ToString() : null
                }),

            ValidationException validationException => new ErrorResponse(
                StatusCodes.Status422UnprocessableEntity,
                new
                {
                    errors = BuildValidationErrors(validationException),
                    detail = isDevelopment ? validationException.ToString() : null
                }),

            _ => isDevelopment
                ? new ErrorResponse(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        error = exception.Message,
                        stackTrace = exception.ToString()
                    })
                : new ErrorResponse(
                    StatusCodes.Status500InternalServerError,
                    new { error = "Internal server error" })
        };
    }

    private static List<string> BuildValidationErrors(ValidationException exception)
    {
        var errors = new List<string>();

        if (!string.IsNullOrWhiteSpace(exception.ValidationResult?.ErrorMessage))
        {
            errors.Add(exception.ValidationResult.ErrorMessage);
        }

        if (!string.IsNullOrWhiteSpace(exception.Message)
            && !errors.Contains(exception.Message, StringComparer.Ordinal))
        {
            errors.Add(exception.Message);
        }

        if (errors.Count == 0)
        {
            errors.Add("Validation failed.");
        }

        return errors;
    }

    private sealed record ErrorResponse(int StatusCode, object Payload);
}
