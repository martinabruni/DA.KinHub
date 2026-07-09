using FluentValidation;
using Kin.KinHub.KinList.Business.Common;
using Microsoft.Net.Http.Headers;

namespace Kin.KinHub.App.Functions.KinListFeature;

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
