using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using Polly.Timeout;
using System.Net;
using WaglBackend.Core.Molecules.Configurations;

namespace WaglBackend.Infrastructure.Services.Resilience;

/// <summary>
/// Service for creating and managing resilience policies for UAI API calls
/// Implements retry, circuit breaker, and timeout policies using Polly
/// </summary>
public class UAIResilienceService
{
    private readonly UAIConfiguration _config;
    private readonly ILogger<UAIResilienceService> _logger;
    private readonly IAsyncPolicy<HttpResponseMessage> _resiliencePolicy;

    public UAIResilienceService(IOptions<UAIConfiguration> config, ILogger<UAIResilienceService> logger)
    {
        _config = config.Value;
        _logger = logger;
        _resiliencePolicy = CreateResiliencePolicy();
    }

    /// <summary>
    /// Gets the configured resilience policy for UAI API calls
    /// </summary>
    public IAsyncPolicy<HttpResponseMessage> GetResiliencePolicy() => _resiliencePolicy;

    /// <summary>
    /// Creates a comprehensive resilience policy combining retry, circuit breaker, and timeout
    /// </summary>
    private IAsyncPolicy<HttpResponseMessage> CreateResiliencePolicy()
    {
        // Retry Policy with Exponential Backoff and Jitter
        var retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .Or<TimeoutRejectedException>()
            .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode && ShouldRetry(r.StatusCode))
            .WaitAndRetryAsync(
                retryCount: _config.RetryPolicy.MaxAttempts - 1, // First attempt doesn't count as retry
                sleepDurationProvider: retryAttempt => CalculateDelay(retryAttempt),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var exception = outcome.Exception;
                    var result = outcome.Result;

                    if (exception != null)
                    {
                        _logger.LogWarning("UAI API call failed with exception on attempt {RetryCount}/{MaxAttempts}. " +
                                         "Retrying in {Delay}ms. Exception: {Exception}",
                            retryCount, _config.RetryPolicy.MaxAttempts, timespan.TotalMilliseconds, exception.Message);
                    }
                    else if (result != null)
                    {
                        _logger.LogWarning("UAI API call failed with status {StatusCode} on attempt {RetryCount}/{MaxAttempts}. " +
                                         "Retrying in {Delay}ms",
                            result.StatusCode, retryCount, _config.RetryPolicy.MaxAttempts, timespan.TotalMilliseconds);
                    }
                });

        // Circuit Breaker Policy
        var circuitBreakerPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .Or<TimeoutRejectedException>()
            .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode && IsServerError(r.StatusCode))
            .AdvancedCircuitBreakerAsync(
                failureThreshold: 0.5, // 50% failure rate
                samplingDuration: TimeSpan.FromMilliseconds(_config.RetryPolicy.CircuitBreakerSamplingDurationMs),
                minimumThroughput: _config.RetryPolicy.CircuitBreakerMinimumThroughput,
                durationOfBreak: TimeSpan.FromMilliseconds(_config.RetryPolicy.CircuitBreakerDurationOfBreakMs),
                onBreak: (exception, duration) =>
                {
                    _logger.LogError("UAI circuit breaker opened for {Duration}ms due to: {Exception}",
                        duration.TotalMilliseconds, exception?.ToString() ?? "Multiple failures");
                },
                onReset: () =>
                {
                    _logger.LogInformation("UAI circuit breaker reset - service appears to be recovered");
                },
                onHalfOpen: () =>
                {
                    _logger.LogInformation("UAI circuit breaker half-open - testing service availability");
                });

        // Timeout Policy
        var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(
            timeout: TimeSpan.FromMilliseconds(_config.TimeoutMs),
            timeoutStrategy: TimeoutStrategy.Optimistic);

        // Combine all policies: Timeout -> CircuitBreaker -> Retry
        return Policy.WrapAsync(timeoutPolicy, circuitBreakerPolicy, retryPolicy);
    }

    /// <summary>
    /// Calculates delay for retry attempts with exponential backoff and optional jitter
    /// </summary>
    private TimeSpan CalculateDelay(int retryAttempt)
    {
        var baseDelay = _config.RetryPolicy.BaseDelayMs;
        var multiplier = _config.RetryPolicy.BackoffMultiplier;
        var maxDelay = _config.RetryPolicy.MaxDelayMs;

        double delay;

        if (_config.RetryPolicy.EnableExponentialBackoff)
        {
            // Exponential backoff: delay = baseDelay * (multiplier ^ (retryAttempt - 1))
            delay = baseDelay * Math.Pow(multiplier, retryAttempt - 1);
        }
        else
        {
            // Linear backoff: delay = baseDelay * retryAttempt
            delay = baseDelay * retryAttempt;
        }

        // Apply maximum delay limit
        delay = Math.Min(delay, maxDelay);

        // Add jitter to prevent thundering herd problem
        if (_config.RetryPolicy.EnableJitter)
        {
            var random = new Random();
            var jitterPercent = 0.1; // ±10% jitter
            var jitterRange = delay * jitterPercent;
            var jitterOffset = random.NextDouble() * jitterRange * 2 - jitterRange;
            delay += jitterOffset;

            // Ensure delay doesn't go below minimum or above maximum
            delay = Math.Max(delay, baseDelay * 0.5);
            delay = Math.Min(delay, maxDelay);
        }

        return TimeSpan.FromMilliseconds(delay);
    }

    /// <summary>
    /// Determines if an HTTP status code should trigger a retry
    /// </summary>
    private static bool ShouldRetry(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            // Retry on server errors and some client errors
            HttpStatusCode.InternalServerError => true,
            HttpStatusCode.BadGateway => true,
            HttpStatusCode.ServiceUnavailable => true,
            HttpStatusCode.GatewayTimeout => true,
            HttpStatusCode.RequestTimeout => true,
            HttpStatusCode.TooManyRequests => true,

            // Don't retry on client errors that won't change
            HttpStatusCode.BadRequest => false,
            HttpStatusCode.Unauthorized => false,
            HttpStatusCode.Forbidden => false,
            HttpStatusCode.NotFound => false,
            HttpStatusCode.Conflict => false,
            HttpStatusCode.UnprocessableEntity => false,

            // Default to not retrying
            _ => false
        };
    }

    /// <summary>
    /// Determines if an HTTP status code represents a server error for circuit breaker
    /// </summary>
    private static bool IsServerError(HttpStatusCode statusCode)
    {
        return (int)statusCode >= 500;
    }

    /// <summary>
    /// Gets metrics about the current state of the resilience policies
    /// </summary>
    public UAIResilienceMetrics GetMetrics()
    {
        // Note: In a production environment, you might want to use a more sophisticated
        // metrics collection system like Prometheus or Application Insights
        return new UAIResilienceMetrics
        {
            MaxRetryAttempts = _config.RetryPolicy.MaxAttempts,
            BaseDelayMs = _config.RetryPolicy.BaseDelayMs,
            MaxDelayMs = _config.RetryPolicy.MaxDelayMs,
            CircuitBreakerFailureThreshold = _config.RetryPolicy.CircuitBreakerFailureThreshold,
            CircuitBreakerDurationOfBreakMs = _config.RetryPolicy.CircuitBreakerDurationOfBreakMs,
            TimeoutMs = _config.TimeoutMs,
            IsExponentialBackoffEnabled = _config.RetryPolicy.EnableExponentialBackoff,
            IsJitterEnabled = _config.RetryPolicy.EnableJitter
        };
    }
}

/// <summary>
/// Metrics class for UAI resilience policies
/// </summary>
public class UAIResilienceMetrics
{
    public int MaxRetryAttempts { get; set; }
    public int BaseDelayMs { get; set; }
    public int MaxDelayMs { get; set; }
    public int CircuitBreakerFailureThreshold { get; set; }
    public int CircuitBreakerDurationOfBreakMs { get; set; }
    public int TimeoutMs { get; set; }
    public bool IsExponentialBackoffEnabled { get; set; }
    public bool IsJitterEnabled { get; set; }
}