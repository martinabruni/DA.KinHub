using FluentValidation;
using Kin.KinHub.KinList.Business.Common;
using Kin.KinHub.KinList.Business.KinListFeature;

namespace Kin.KinHub.KinList.Api.KinListFeature;

public sealed class CreateKinListRequestValidator : AbstractValidator<CreateKinListRequest>
{
    public CreateKinListRequestValidator(KinListOptions options)
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(options.MaxTitleLength);
        RuleFor(x => x.Items).Must(x => x.Count <= options.MaxItemsPerBulkConfirm).WithMessage($"A list draft can contain at most {options.MaxItemsPerBulkConfirm} items.");
        RuleForEach(x => x.Items).NotEmpty().MaximumLength(options.MaxItemLength);
    }
}
