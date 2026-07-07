using FluentValidation;
using Kin.KinHub.KinList.Business.Common;
using Kin.KinHub.KinList.Business.KinListFeature;

namespace Kin.KinHub.KinList.Api.KinListFeature;

public sealed class UpdateKinListItemRequestValidator : AbstractValidator<UpdateKinListItemRequest>
{
    public UpdateKinListItemRequestValidator(KinListOptions options)
    {
        RuleFor(x => x.Text).NotEmpty().MaximumLength(options.MaxItemLength);
    }
}
