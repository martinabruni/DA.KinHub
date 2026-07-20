namespace AdvancedFrontier.Business.Common;

public sealed class BusinessAccessDeniedException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
