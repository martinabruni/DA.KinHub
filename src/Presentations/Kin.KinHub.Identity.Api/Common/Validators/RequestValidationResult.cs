namespace Kin.KinHub.Identity.Api.Common.Validators;

public sealed class RequestValidationResult
{
    public bool IsValid { get; }
    public IReadOnlyList<string> Errors { get; }

    private RequestValidationResult(bool isValid, IReadOnlyList<string> errors)
    {
        IsValid = isValid;
        Errors = errors;
    }

    public static RequestValidationResult Success() =>
        new(true, []);

    public static RequestValidationResult Failure(IReadOnlyList<string> errors) =>
        new(false, errors);
}
