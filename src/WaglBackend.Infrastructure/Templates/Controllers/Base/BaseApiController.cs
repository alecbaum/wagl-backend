using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WaglBackend.Core.Atoms.Constants;
using CustomClaimTypes = WaglBackend.Core.Atoms.Constants.ClaimTypes;

namespace WaglBackend.Infrastructure.Templates.Controllers.Base;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{
    protected ILogger Logger { get; }

    protected BaseApiController(ILogger logger)
    {
        Logger = logger;
    }

    protected ActionResult<T> HandleResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return Ok(result.Value);

        return result.ErrorCode switch
        {
            ErrorCodes.NotFoundError => NotFound(new { error = result.ErrorCode, message = result.ErrorMessage }),
            ErrorCodes.ValidationError => BadRequest(new { error = result.ErrorCode, message = result.ErrorMessage }),
            ErrorCodes.UnauthorizedError => Unauthorized(new { error = result.ErrorCode, message = result.ErrorMessage }),
            ErrorCodes.ForbiddenError => Forbid(),
            ErrorCodes.RateLimitError => StatusCode(429, new { error = result.ErrorCode, message = result.ErrorMessage }),
            ErrorCodes.ConflictError => Conflict(new { error = result.ErrorCode, message = result.ErrorMessage }),
            _ => StatusCode(500, new { error = "INTERNAL_SERVER_ERROR", message = "An unexpected error occurred" })
        };
    }

    protected string GetUserId() => User.FindFirst(CustomClaimTypes.UserId)?.Value ?? string.Empty;
    
    protected string GetUserEmail() => User.FindFirst(CustomClaimTypes.Email)?.Value ?? string.Empty;
    
    protected string GetAccountType() => User.FindFirst(CustomClaimTypes.AccountType)?.Value ?? string.Empty;
    
    protected string GetTierLevel() => User.FindFirst(CustomClaimTypes.TierLevel)?.Value ?? string.Empty;
    
    protected bool IsProvider() => GetAccountType() == "Provider";
    
    protected bool IsUser() => GetAccountType() == "User";
}

public class Result<T>
{
    public bool IsSuccess { get; set; }
    public T? Value { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;

    public static Result<T> Success(T value) => new() { IsSuccess = true, Value = value };
    
    public static Result<T> Failure(string errorMessage, string errorCode) => new() 
    { 
        IsSuccess = false, 
        ErrorMessage = errorMessage, 
        ErrorCode = errorCode 
    };
}