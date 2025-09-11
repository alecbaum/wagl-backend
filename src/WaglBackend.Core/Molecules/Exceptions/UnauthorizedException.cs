using WaglBackend.Core.Atoms.Constants;

namespace WaglBackend.Core.Molecules.Exceptions;

public class UnauthorizedException : Exception
{
    public string ErrorCode { get; }

    public UnauthorizedException() 
        : base("Unauthorized access")
    {
        ErrorCode = ErrorCodes.UnauthorizedError;
    }

    public UnauthorizedException(string message) 
        : base(message)
    {
        ErrorCode = ErrorCodes.UnauthorizedError;
    }

    public UnauthorizedException(string message, string errorCode) 
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public UnauthorizedException(string message, Exception innerException) 
        : base(message, innerException)
    {
        ErrorCode = ErrorCodes.UnauthorizedError;
    }

    public UnauthorizedException(string message, string errorCode, Exception innerException) 
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}