using WaglBackend.Core.Atoms.Constants;

namespace WaglBackend.Core.Molecules.Exceptions;

public class BusinessRuleException : Exception
{
    public string ErrorCode { get; }
    public string RuleName { get; }
    public Dictionary<string, object> Context { get; }

    public BusinessRuleException(string ruleName) 
        : base($"Business rule violation: {ruleName}")
    {
        ErrorCode = ErrorCodes.GeneralError;
        RuleName = ruleName;
        Context = new Dictionary<string, object>();
    }

    public BusinessRuleException(string ruleName, string message) 
        : base(message)
    {
        ErrorCode = ErrorCodes.GeneralError;
        RuleName = ruleName;
        Context = new Dictionary<string, object>();
    }

    public BusinessRuleException(string ruleName, string message, string errorCode) 
        : base(message)
    {
        ErrorCode = errorCode;
        RuleName = ruleName;
        Context = new Dictionary<string, object>();
    }

    public BusinessRuleException(string ruleName, string message, Dictionary<string, object> context) 
        : base(message)
    {
        ErrorCode = ErrorCodes.GeneralError;
        RuleName = ruleName;
        Context = context;
    }

    public BusinessRuleException(string ruleName, string message, string errorCode, Dictionary<string, object> context) 
        : base(message)
    {
        ErrorCode = errorCode;
        RuleName = ruleName;
        Context = context;
    }

    public BusinessRuleException(string ruleName, string message, Exception innerException) 
        : base(message, innerException)
    {
        ErrorCode = ErrorCodes.GeneralError;
        RuleName = ruleName;
        Context = new Dictionary<string, object>();
    }
}