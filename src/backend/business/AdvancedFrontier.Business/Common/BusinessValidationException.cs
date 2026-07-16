namespace AdvancedFrontier.Business.Common;

public sealed class BusinessValidationException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
