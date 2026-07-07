using FluentValidation;
using Kin.KinHub.KinList.Business.Common;
using Kin.KinHub.KinList.Business.KinListFeature;

namespace Kin.KinHub.KinList.Api.KinListFeature;

public sealed class CreateKinListItemRequestValidator : AbstractValidator<CreateKinListItemRequest>
{
    public CreateKinListItemRequestValidator(KinListOptions options)
    {
        RuleFor(x => x.Text).NotEmpty().MaximumLength(options.MaxItemLength);
    }
}
