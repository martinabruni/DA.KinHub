using FluentValidation;

namespace Kin.KinHub.App.Functions.FamilyFeature;

internal sealed class ToggleFamilyServiceRequestValidator : AbstractValidator<ToggleFamilyServiceRequest>
{
    public ToggleFamilyServiceRequestValidator()
    {
        RuleFor(x => x.ServiceId)
            .GreaterThan(0);
    }
}
