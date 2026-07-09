
namespace Kin.KinHub.Identity.Api.Common.Validators;

public interface IRequestValidator<T>
{
    Task<RequestValidationResult> ValidateAsync(T request, CancellationToken cancellationToken = default);
}
