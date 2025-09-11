using WaglBackend.Core.Atoms.Constants;

namespace WaglBackend.Core.Molecules.Exceptions;

public class InvalidApiKeyException : Exception
{
    public string ErrorCode { get; }
    public string? ApiKeyPreview { get; }

    public InvalidApiKeyException() 
        : base("Invalid API key provided")
    {
        ErrorCode = ErrorCodes.Authentication.InvalidApiKey;
    }

    public InvalidApiKeyException(string message) 
        : base(message)
    {
        ErrorCode = ErrorCodes.Authentication.InvalidApiKey;
    }

    public InvalidApiKeyException(string message, string? apiKeyPreview) 
        : base(message)
    {
        ErrorCode = ErrorCodes.Authentication.InvalidApiKey;
        ApiKeyPreview = apiKeyPreview;
    }

    public InvalidApiKeyException(string message, Exception innerException) 
        : base(message, innerException)
    {
        ErrorCode = ErrorCodes.Authentication.InvalidApiKey;
    }

    public InvalidApiKeyException(string message, string? apiKeyPreview, Exception innerException) 
        : base(message, innerException)
    {
        ErrorCode = ErrorCodes.Authentication.InvalidApiKey;
        ApiKeyPreview = apiKeyPreview;
    }
}