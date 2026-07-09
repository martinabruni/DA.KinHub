using FluentValidation;

namespace Kin.KinHub.App.Functions.Common.Validators;

public sealed class FluentRequestValidator<T> : IRequestValidator<T>
{
    private readonly IValidator<T> _validator;

    public FluentRequestValidator(IValidator<T> validator)
    {
        _validator = validator;
    }

    public async Task<RequestValidationResult> ValidateAsync(T request, CancellationToken cancellationToken = default)
    {
        var result = await _validator.ValidateAsync(request, cancellationToken);

        if (result.IsValid)
            return RequestValidationResult.Success();

        var errors = result.Errors
            .Select(e => e.ErrorMessage)
            .ToList();

        return RequestValidationResult.Failure(errors);
    }
}
