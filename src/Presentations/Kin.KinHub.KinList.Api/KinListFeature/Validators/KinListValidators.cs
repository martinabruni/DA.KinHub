using FluentValidation;
using Kin.KinHub.KinList.Business.Common;
using Kin.KinHub.KinList.Business.KinListFeature;
using Microsoft.Net.Http.Headers;

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

public sealed class UpdateKinListRequestValidator : AbstractValidator<UpdateKinListRequest>
{
    public UpdateKinListRequestValidator(KinListOptions options)
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(options.MaxTitleLength);
    }
}

public sealed class CreateKinListItemRequestValidator : AbstractValidator<CreateKinListItemRequest>
{
    public CreateKinListItemRequestValidator(KinListOptions options)
    {
        RuleFor(x => x.Text).NotEmpty().MaximumLength(options.MaxItemLength);
    }
}

public sealed class BulkConfirmKinListItemsRequestValidator : AbstractValidator<BulkConfirmKinListItemsRequest>
{
    public BulkConfirmKinListItemsRequestValidator(KinListOptions options)
    {
        RuleFor(x => x.Items).NotEmpty().Must(x => x.Count <= options.MaxItemsPerBulkConfirm).WithMessage($"A bulk confirm operation can contain at most {options.MaxItemsPerBulkConfirm} items.");
        RuleForEach(x => x.Items).NotEmpty().MaximumLength(options.MaxItemLength);
    }
}

public sealed class UpdateKinListItemRequestValidator : AbstractValidator<UpdateKinListItemRequest>
{
    public UpdateKinListItemRequestValidator(KinListOptions options)
    {
        RuleFor(x => x.Text).NotEmpty().MaximumLength(options.MaxItemLength);
    }
}

public sealed class KinListAudioFormRequestValidator : AbstractValidator<KinListAudioFormRequest>
{
    public KinListAudioFormRequestValidator(KinListOptions options)
    {
        RuleFor(x => x.Audio).NotNull();
        RuleFor(x => x.Audio!)
            .Must(file => file.Length > 0)
            .When(x => x.Audio is not null)
            .WithMessage("Audio payload cannot be empty.");
        RuleFor(x => x.Audio!)
            .Must(file => file.Length <= options.MaxAudioBytes)
            .When(x => x.Audio is not null)
            .WithMessage($"Audio payload cannot exceed {options.MaxAudioBytes} bytes.");
        RuleFor(x => x.Audio!)
            .Must(file => HasAllowedAudioContentType(file.ContentType, options.AllowedAudioMimeTypes))
            .When(x => x.Audio is not null)
            .WithMessage($"Audio content type must be one of: {string.Join(", ", options.AllowedAudioMimeTypes)}.");
    }

    private static bool HasAllowedAudioContentType(string? contentType, string[] allowedMimeTypes)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        if (!MediaTypeHeaderValue.TryParse(contentType, out var parsed) || string.IsNullOrWhiteSpace(parsed.MediaType.Value))
        {
            return false;
        }

        return allowedMimeTypes.Contains(parsed.MediaType.Value, StringComparer.OrdinalIgnoreCase);
    }
}
