using SearchAutocomplete.Application.Exceptions;
using Serilog;
using System.Net;
using System.Text.Json;

namespace SearchAutocomplete.Infrastructure.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = Guid.NewGuid().ToString();
        var requestPath = context.Request.Path.Value;
        var requestMethod = context.Request.Method;
        var userAgent = context.Request.Headers["User-Agent"].ToString();
        
        // Build structured logging context
        var logContext = Log.ForContext("CorrelationId", correlationId)
                            .ForContext("RequestPath", requestPath)
                            .ForContext("RequestMethod", requestMethod)
                            .ForContext("UserAgent", userAgent)
                            .ForContext("ExceptionType", exception.GetType().Name);

        // Add exception-specific context
        switch (exception)
        {
            case SearchException searchEx:
                logContext = logContext.ForContext("SearchTerm", searchEx.SearchTerm)
                                     .ForContext("SearchContext", searchEx.SearchContext, true);
                break;
            case AutocompleteException autocompleteEx:
                logContext = logContext.ForContext("SearchTerm", autocompleteEx.SearchTerm)
                                     .ForContext("MaxResults", autocompleteEx.MaxResults)
                                     .ForContext("AutocompleteContext", autocompleteEx.AutocompleteContext, true);
                break;
        }

        // Log the structured error
        logContext.Error(exception, "Unhandled exception occurred during request processing");

        // Also log using the injected logger for consistency
        _logger.LogError(exception, 
            "Unhandled exception occurred. CorrelationId: {CorrelationId}, Path: {RequestPath}, Method: {RequestMethod}", 
            correlationId, requestPath, requestMethod);

        context.Response.ContentType = "application/json";

        var (statusCode, message, includeDetails) = GetErrorResponse(exception);
        context.Response.StatusCode = statusCode;

        var response = new
        {
            error = new
            {
                message = message,
                correlationId = correlationId,
                timestamp = DateTime.UtcNow,
                details = includeDetails ? exception.Message : null
            }
        };

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        await context.Response.WriteAsync(jsonResponse);
    }

    private static (int statusCode, string message, bool includeDetails) GetErrorResponse(Exception exception)
    {
        return exception switch
        {
            SearchException searchEx => (
                (int)HttpStatusCode.BadRequest,
                "An error occurred while searching. Please check your search criteria and try again.",
                true
            ),
            AutocompleteException autocompleteEx => (
                (int)HttpStatusCode.BadRequest,
                "An error occurred while getting suggestions. Please try again.",
                true
            ),
            ArgumentNullException => (
                (int)HttpStatusCode.BadRequest,
                "Required parameters are missing. Please check your request.",
                false
            ),
            ArgumentException => (
                (int)HttpStatusCode.BadRequest,
                "Invalid request parameters provided. Please check your input.",
                false
            ),
            UnauthorizedAccessException => (
                (int)HttpStatusCode.Unauthorized,
                "Access denied. Please check your permissions.",
                false
            ),
            TimeoutException => (
                (int)HttpStatusCode.RequestTimeout,
                "The request timed out. Please try again.",
                false
            ),
            _ => (
                (int)HttpStatusCode.InternalServerError,
                "An unexpected error occurred. Please try again later.",
                false
            )
        };
    }
}