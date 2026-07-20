namespace AdvancedFrontier.Business.Common;

public sealed class BusinessDependencyException(string code, string message, Exception? innerException = null) : Exception(message, innerException)
{
    public string Code { get; } = code;
}
