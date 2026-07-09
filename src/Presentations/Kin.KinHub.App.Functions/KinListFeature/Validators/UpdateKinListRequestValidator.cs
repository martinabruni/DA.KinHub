using FluentValidation;
using Kin.KinHub.KinList.Business.Common;
using Kin.KinHub.KinList.Business.KinListFeature;

namespace Kin.KinHub.App.Functions.KinListFeature;

public sealed class UpdateKinListRequestValidator : AbstractValidator<UpdateKinListRequest>
{
    public UpdateKinListRequestValidator(KinListOptions options)
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(options.MaxTitleLength);
    }
}
