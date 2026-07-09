
namespace Kin.KinHub.App.Functions.Common.Validators;

public interface IRequestValidator<T>
{
    Task<RequestValidationResult> ValidateAsync(T request, CancellationToken cancellationToken = default);
}
