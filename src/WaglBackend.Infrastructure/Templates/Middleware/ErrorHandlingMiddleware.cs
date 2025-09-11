using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using WaglBackend.Core.Atoms.Constants;
using WaglBackend.Core.Molecules.Exceptions;

namespace WaglBackend.Infrastructure.Templates.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
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
            _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse();

        switch (exception)
        {
            case UnauthorizedException unauthorizedException:
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                response.Error = unauthorizedException.ErrorCode;
                response.Message = unauthorizedException.Message;
                break;

            case TierLimitExceededException tierException:
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                response.Error = tierException.ErrorCode;
                response.Message = tierException.Message;
                response.Details = new Dictionary<string, object>
                {
                    ["currentTier"] = tierException.CurrentTier.ToString(),
                    ["requiredTier"] = tierException.RequiredTier.ToString()
                };
                break;

            case InvalidApiKeyException apiKeyException:
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                response.Error = apiKeyException.ErrorCode;
                response.Message = apiKeyException.Message;
                if (!string.IsNullOrEmpty(apiKeyException.ApiKeyPreview))
                {
                    response.Details = new Dictionary<string, object>
                    {
                        ["apiKeyPreview"] = apiKeyException.ApiKeyPreview
                    };
                }
                break;

            case BusinessRuleException businessException:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Error = businessException.ErrorCode;
                response.Message = businessException.Message;
                response.Details = new Dictionary<string, object>
                {
                    ["ruleName"] = businessException.RuleName,
                    ["context"] = businessException.Context
                };
                break;

            case ArgumentException:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Error = ErrorCodes.ValidationError;
                response.Message = exception.Message;
                break;

            case UnauthorizedAccessException:
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                response.Error = ErrorCodes.UnauthorizedError;
                response.Message = "Access denied";
                break;

            case TimeoutException:
                context.Response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                response.Error = ErrorCodes.GeneralError;
                response.Message = "Request timeout";
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.Error = ErrorCodes.GeneralError;
                response.Message = "An internal server error occurred";
                break;
        }

        response.TraceId = context.TraceIdentifier;
        response.Timestamp = DateTime.UtcNow;

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }

    private class ErrorResponse
    {
        public string Error { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, object>? Details { get; set; }
        public string TraceId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}