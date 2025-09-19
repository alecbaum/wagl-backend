using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text;
using System.Text.Json;
using WaglBackend.Core.Atoms.Entities;
using WaglBackend.Core.Atoms.ValueObjects;
using WaglBackend.Core.Molecules.Configurations;
using WaglBackend.Core.Molecules.DTOs.Request.UAI;
using WaglBackend.Domain.Organisms.Services;

namespace WaglBackend.Infrastructure.Services;

/// <summary>
/// Service for integrating with UAI (Unanimous AI) system
/// Handles all outbound communication to UAI endpoints with enhanced error handling
/// </summary>
public class UAIIntegrationService : IUAIIntegrationService
{
    private readonly HttpClient _httpClient;
    private readonly UAIConfiguration _config;
    private readonly ILogger<UAIIntegrationService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    // Enhanced Error Handling State
    private DateTime _lastFailureTime = DateTime.MinValue;
    private int _consecutiveFailures = 0;
    private int _totalRequests = 0;
    private int _totalFailures = 0;
    private readonly object _stateLock = new object();

    public UAIIntegrationService(
        HttpClient httpClient,
        IOptions<UAIConfiguration> config,
        ILogger<UAIIntegrationService> logger)
    {
        _httpClient = httpClient;
        _config = config.Value;
        _logger = logger;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        // Configure HttpClient
        _httpClient.BaseAddress = new Uri(_config.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromMilliseconds(_config.TimeoutMs);
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        if (!_config.EnableIntegration)
        {
            _logger.LogWarning("UAI integration is disabled in configuration");
            return false;
        }

        try
        {
            // Health checks use room 0 exclusively - real chat rooms use rooms 1, 2, 3
            var healthRequest = new UAIHealthCheckRequest
            {
                Room = GetHealthCheckRoomNumber(),
                UserID = 0,
                Message = "Health Check"
            };
            var endpoint = $"/{_config.TestSessionId}{_config.Endpoints.Health}";

            _logger.LogDebug("Checking UAI health at endpoint: {Endpoint} using room {Room}", endpoint, healthRequest.Room);

            var response = await PostAsync(endpoint, healthRequest, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UAI health check failed");
            return false;
        }
    }

    public async Task<bool> SendMessageAsync(ChatMessage message, string uaiSessionId, int uaiRoomNumber, CancellationToken cancellationToken = default)
    {
        if (!_config.EnableIntegration)
        {
            _logger.LogDebug("UAI integration disabled, skipping message send for message {MessageId}", message.Id);
            return true; // Return true to not block normal flow
        }

        try
        {
            var request = new UAIMessageSendRequest
            {
                Message = message.Content,
                UserID = GetUAIUserId(message.ParticipantId),
                Room = uaiRoomNumber
            };

            var endpoint = $"/{uaiSessionId}{_config.Endpoints.MessageSend}";

            _logger.LogInformation("Sending message to UAI: MessageId={MessageId}, UAISession={UAISession}, Room={Room}, ParticipantId={ParticipantId}",
                message.Id, uaiSessionId, uaiRoomNumber, message.ParticipantId);

            var response = await PostAsync(endpoint, request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Successfully sent message {MessageId} to UAI", message.Id);
                return true;
            }

            _logger.LogWarning("Failed to send message {MessageId} to UAI. Status: {StatusCode}",
                message.Id, response.StatusCode);
            return false;
        }
        catch (UAIServiceUnavailableException ex)
        {
            _logger.LogWarning(ex, "UAI service unavailable for message {MessageId} - circuit breaker open", message.Id);
            return false; // Graceful degradation - don't block chat functionality
        }
        catch (UAITimeoutException ex)
        {
            _logger.LogWarning(ex, "UAI timeout sending message {MessageId} - continuing without UAI", message.Id);
            return false;
        }
        catch (UAINetworkException ex)
        {
            _logger.LogWarning(ex, "UAI network error sending message {MessageId} - continuing without UAI", message.Id);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending message {MessageId} to UAI", message.Id);
            return false;
        }
    }

    public async Task<bool> NotifyUserConnectAsync(Participant participant, string uaiSessionId, int uaiRoomNumber, CancellationToken cancellationToken = default)
    {
        if (!_config.EnableIntegration)
        {
            _logger.LogDebug("UAI integration disabled, skipping user connect for participant {ParticipantId}", participant.Id);
            return true;
        }

        try
        {
            var request = new UAIUserConnectRequest
            {
                Username = participant.DisplayName,
                UniqueID = GetUAIUserId(participant.Id),
                UrlParams = _config.DefaultUrlParams,
                Room = uaiRoomNumber
            };

            var endpoint = $"/{uaiSessionId}{_config.Endpoints.UserConnect}";

            _logger.LogInformation("Notifying UAI of user connect: ParticipantId={ParticipantId}, DisplayName={DisplayName}, UAISession={UAISession}, Room={Room}",
                participant.Id, participant.DisplayName, uaiSessionId, uaiRoomNumber);

            var response = await PostAsync(endpoint, request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Successfully notified UAI of user connect for participant {ParticipantId}", participant.Id);
                return true;
            }

            _logger.LogWarning("Failed to notify UAI of user connect for participant {ParticipantId}. Status: {StatusCode}",
                participant.Id, response.StatusCode);
            return false;
        }
        catch (UAIServiceUnavailableException ex)
        {
            _logger.LogWarning(ex, "UAI service unavailable for user connect {ParticipantId} - circuit breaker open", participant.Id);
            return false;
        }
        catch (UAITimeoutException ex)
        {
            _logger.LogWarning(ex, "UAI timeout for user connect {ParticipantId} - continuing without UAI", participant.Id);
            return false;
        }
        catch (UAINetworkException ex)
        {
            _logger.LogWarning(ex, "UAI network error for user connect {ParticipantId} - continuing without UAI", participant.Id);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error notifying UAI of user connect for participant {ParticipantId}", participant.Id);
            return false;
        }
    }

    public async Task<bool> NotifyUserDisconnectAsync(Participant participant, string uaiSessionId, int uaiRoomNumber, CancellationToken cancellationToken = default)
    {
        if (!_config.EnableIntegration)
        {
            _logger.LogDebug("UAI integration disabled, skipping user disconnect for participant {ParticipantId}", participant.Id);
            return true;
        }

        try
        {
            var request = new UAIUserDisconnectRequest
            {
                Username = participant.DisplayName,
                UniqueID = GetUAIUserId(participant.Id),
                UrlParams = _config.DefaultUrlParams,
                Room = uaiRoomNumber
            };

            var endpoint = $"/{uaiSessionId}{_config.Endpoints.UserDisconnect}";

            _logger.LogInformation("Notifying UAI of user disconnect: ParticipantId={ParticipantId}, DisplayName={DisplayName}, UAISession={UAISession}, Room={Room}",
                participant.Id, participant.DisplayName, uaiSessionId, uaiRoomNumber);

            var response = await PostAsync(endpoint, request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Successfully notified UAI of user disconnect for participant {ParticipantId}", participant.Id);
                return true;
            }

            _logger.LogWarning("Failed to notify UAI of user disconnect for participant {ParticipantId}. Status: {StatusCode}",
                participant.Id, response.StatusCode);
            return false;
        }
        catch (UAIServiceUnavailableException ex)
        {
            _logger.LogWarning(ex, "UAI service unavailable for user disconnect {ParticipantId} - circuit breaker open", participant.Id);
            return false;
        }
        catch (UAITimeoutException ex)
        {
            _logger.LogWarning(ex, "UAI timeout for user disconnect {ParticipantId} - continuing without UAI", participant.Id);
            return false;
        }
        catch (UAINetworkException ex)
        {
            _logger.LogWarning(ex, "UAI network error for user disconnect {ParticipantId} - continuing without UAI", participant.Id);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error notifying UAI of user disconnect for participant {ParticipantId}", participant.Id);
            return false;
        }
    }

    public string GetUAISessionId(SessionId sessionId)
    {
        // For now, all sessions map to the test session
        // TODO: Implement proper session mapping when UAI supports session creation
        return _config.TestSessionId;
    }

    public int GetUAIRoomNumber(RoomId roomId)
    {
        // Simple hash-based room assignment to distribute across available rooms (1, 2, 3)
        // Room 0 is reserved for health checks only
        var hash = roomId.Value.GetHashCode();
        var roomIndex = Math.Abs(hash) % _config.AvailableRooms.Length;
        return _config.AvailableRooms[roomIndex];
    }

    public int GetHealthCheckRoomNumber()
    {
        // Room 0 is reserved exclusively for health checks
        return _config.HealthCheckRoom;
    }

    public long GetUAIUserId(Guid participantId)
    {
        // Convert GUID to a consistent long value for UAI
        // Use first 8 bytes of GUID to create a long
        var bytes = participantId.ToByteArray();
        return Math.Abs(BitConverter.ToInt64(bytes, 0));
    }

    private async Task<HttpResponseMessage> PostAsync<T>(string endpoint, T data, CancellationToken cancellationToken)
    {
        // Check circuit breaker before making request
        if (IsCircuitBreakerOpen())
        {
            _logger.LogWarning("UAI circuit breaker is OPEN - rejecting request to {Endpoint}. Last failure: {LastFailure}, Consecutive failures: {ConsecutiveFailures}",
                endpoint, _lastFailureTime, _consecutiveFailures);
            throw new UAIServiceUnavailableException("UAI service circuit breaker is open - service temporarily unavailable");
        }

        var json = JsonSerializer.Serialize(data, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogDebug("POST {Endpoint}: {Json}", endpoint, json);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsync(endpoint, content, cancellationToken);

            // Track successful request
            RecordRequestResult(true, response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("UAI API call failed. Endpoint: {Endpoint}, Status: {StatusCode}, Response: {Response}",
                    endpoint, response.StatusCode, responseContent);

                // Categorize and handle the error
                HandleHttpError(response.StatusCode, endpoint, responseContent);
            }

            return response;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            RecordRequestResult(false, null);
            _logger.LogError(ex, "UAI request timeout for endpoint {Endpoint} after {Timeout}ms", endpoint, _config.TimeoutMs);
            throw new UAITimeoutException($"UAI request timed out after {_config.TimeoutMs}ms", ex);
        }
        catch (HttpRequestException ex)
        {
            RecordRequestResult(false, null);
            _logger.LogError(ex, "UAI network error for endpoint {Endpoint}", endpoint);
            throw new UAINetworkException($"Network error contacting UAI: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            RecordRequestResult(false, null);
            _logger.LogError(ex, "Unexpected error calling UAI endpoint {Endpoint}", endpoint);
            throw new UAIUnexpectedException($"Unexpected error calling UAI: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Checks if the circuit breaker should block requests due to consecutive failures
    /// </summary>
    private bool IsCircuitBreakerOpen()
    {
        lock (_stateLock)
        {
            // Circuit breaker thresholds
            const int maxConsecutiveFailures = 5;
            var circuitBreakerTimeoutMinutes = 5;

            // If we haven't hit the failure threshold, circuit is closed
            if (_consecutiveFailures < maxConsecutiveFailures)
                return false;

            // If enough time has passed since last failure, allow one test request
            var timeSinceLastFailure = DateTime.UtcNow - _lastFailureTime;
            if (timeSinceLastFailure.TotalMinutes >= circuitBreakerTimeoutMinutes)
            {
                _logger.LogInformation("UAI circuit breaker allowing test request after {Minutes} minutes",
                    timeSinceLastFailure.TotalMinutes);
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Records the result of a UAI request for circuit breaker and monitoring
    /// </summary>
    private void RecordRequestResult(bool success, HttpStatusCode? statusCode)
    {
        lock (_stateLock)
        {
            _totalRequests++;

            if (success)
            {
                if (_consecutiveFailures > 0)
                {
                    _logger.LogInformation("UAI service recovered - resetting failure count from {FailureCount}", _consecutiveFailures);
                    _consecutiveFailures = 0;
                }
            }
            else
            {
                _totalFailures++;
                _consecutiveFailures++;
                _lastFailureTime = DateTime.UtcNow;

                var failureRate = (double)_totalFailures / _totalRequests * 100;
                _logger.LogWarning("UAI request failed - Consecutive failures: {ConsecutiveFailures}, Total failure rate: {FailureRate:F1}% ({TotalFailures}/{TotalRequests})",
                    _consecutiveFailures, failureRate, _totalFailures, _totalRequests);
            }
        }
    }

    /// <summary>
    /// Categorizes and handles different types of HTTP errors from UAI
    /// </summary>
    private void HandleHttpError(HttpStatusCode statusCode, string endpoint, string responseContent)
    {
        switch (statusCode)
        {
            case HttpStatusCode.BadRequest:
                _logger.LogError("UAI Bad Request (400) for {Endpoint}: {Response}. This indicates a client-side error in our request format.",
                    endpoint, responseContent);
                break;

            case HttpStatusCode.Unauthorized:
                _logger.LogError("UAI Unauthorized (401) for {Endpoint}: {Response}. Check authentication credentials.",
                    endpoint, responseContent);
                break;

            case HttpStatusCode.Forbidden:
                _logger.LogError("UAI Forbidden (403) for {Endpoint}: {Response}. Check API permissions.",
                    endpoint, responseContent);
                break;

            case HttpStatusCode.NotFound:
                _logger.LogError("UAI Not Found (404) for {Endpoint}: {Response}. Endpoint may not exist or session may be invalid.",
                    endpoint, responseContent);
                break;

            case HttpStatusCode.TooManyRequests:
                _logger.LogWarning("UAI Rate Limited (429) for {Endpoint}: {Response}. Backing off requests.",
                    endpoint, responseContent);
                break;

            case HttpStatusCode.InternalServerError:
            case HttpStatusCode.BadGateway:
            case HttpStatusCode.ServiceUnavailable:
            case HttpStatusCode.GatewayTimeout:
                _logger.LogWarning("UAI Server Error ({StatusCode}) for {Endpoint}: {Response}. UAI service may be experiencing issues.",
                    statusCode, endpoint, responseContent);
                break;

            default:
                _logger.LogWarning("UAI Unexpected Status ({StatusCode}) for {Endpoint}: {Response}",
                    statusCode, endpoint, responseContent);
                break;
        }
    }

    /// <summary>
    /// Gets current error statistics for monitoring
    /// </summary>
    public UAIErrorStatistics GetErrorStatistics()
    {
        lock (_stateLock)
        {
            return new UAIErrorStatistics
            {
                TotalRequests = _totalRequests,
                TotalFailures = _totalFailures,
                ConsecutiveFailures = _consecutiveFailures,
                FailureRate = _totalRequests > 0 ? (double)_totalFailures / _totalRequests * 100 : 0,
                LastFailureTime = _lastFailureTime,
                IsCircuitBreakerOpen = IsCircuitBreakerOpen()
            };
        }
    }
}

/// <summary>
/// UAI-specific exception for service unavailability (circuit breaker open)
/// </summary>
public class UAIServiceUnavailableException : Exception
{
    public UAIServiceUnavailableException(string message) : base(message) { }
    public UAIServiceUnavailableException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// UAI-specific exception for network connectivity issues
/// </summary>
public class UAINetworkException : Exception
{
    public UAINetworkException(string message) : base(message) { }
    public UAINetworkException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// UAI-specific exception for request timeouts
/// </summary>
public class UAITimeoutException : Exception
{
    public UAITimeoutException(string message) : base(message) { }
    public UAITimeoutException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// UAI-specific exception for unexpected errors
/// </summary>
public class UAIUnexpectedException : Exception
{
    public UAIUnexpectedException(string message) : base(message) { }
    public UAIUnexpectedException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Error statistics for UAI service monitoring
/// </summary>
public class UAIErrorStatistics
{
    public int TotalRequests { get; set; }
    public int TotalFailures { get; set; }
    public int ConsecutiveFailures { get; set; }
    public double FailureRate { get; set; }
    public DateTime LastFailureTime { get; set; }
    public bool IsCircuitBreakerOpen { get; set; }
}