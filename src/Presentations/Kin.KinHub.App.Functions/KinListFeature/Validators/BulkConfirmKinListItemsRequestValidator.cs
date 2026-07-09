using FluentValidation;
using Kin.KinHub.KinList.Business.Common;
using Kin.KinHub.KinList.Business.KinListFeature;

namespace Kin.KinHub.App.Functions.KinListFeature;

public sealed class BulkConfirmKinListItemsRequestValidator : AbstractValidator<BulkConfirmKinListItemsRequest>
{
    public BulkConfirmKinListItemsRequestValidator(KinListOptions options)
    {
        RuleFor(x => x.Items).NotEmpty().Must(x => x.Count <= options.MaxItemsPerBulkConfirm).WithMessage($"A bulk confirm operation can contain at most {options.MaxItemsPerBulkConfirm} items.");
        RuleForEach(x => x.Items).NotEmpty().MaximumLength(options.MaxItemLength);
    }
}
