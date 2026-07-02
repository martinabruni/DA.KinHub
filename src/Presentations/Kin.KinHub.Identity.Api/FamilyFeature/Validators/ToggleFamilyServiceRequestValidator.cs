using FluentValidation;

namespace Kin.KinHub.Identity.Api.FamilyFeature;

internal sealed class ToggleFamilyServiceRequestValidator : AbstractValidator<ToggleFamilyServiceRequest>
{
    public ToggleFamilyServiceRequestValidator()
    {
        RuleFor(x => x.ServiceId)
            .GreaterThan(0);
    }
}
