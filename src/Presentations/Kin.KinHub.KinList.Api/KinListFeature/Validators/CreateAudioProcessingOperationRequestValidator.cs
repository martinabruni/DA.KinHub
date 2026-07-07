using FluentValidation;
using Kin.KinHub.KinList.Business.Common;
using Kin.KinHub.KinList.Business.KinListFeature;
using Microsoft.Net.Http.Headers;

namespace Kin.KinHub.KinList.Api.KinListFeature;

public sealed class CreateAudioProcessingOperationRequestValidator : AbstractValidator<CreateAudioProcessingOperationRequest>
{
    public CreateAudioProcessingOperationRequestValidator(KinListOptions options)
    {
        RuleFor(x => x.Type).NotEmpty().Must(type =>
            string.Equals(type, "NewList", StringComparison.OrdinalIgnoreCase)
            || string.Equals(type, "AppendItems", StringComparison.OrdinalIgnoreCase));
        RuleFor(x => x.ContentType).NotEmpty().Must(contentType => HasAllowedContentType(contentType, options.AllowedAudioMimeTypes));
        RuleFor(x => x.DeclaredByteSize).GreaterThan(0).LessThanOrEqualTo(options.MaxAudioBytes);
    }

    private static bool HasAllowedContentType(string? contentType, string[] allowedMimeTypes)
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
