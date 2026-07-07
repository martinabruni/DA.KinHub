using FluentValidation;

namespace Kin.KinHub.KinList.Business.KinListFeature;

public sealed class CreateAudioProcessingOperationBusinessValidator : AbstractValidator<CreateAudioProcessingOperationRequest>
{
    public CreateAudioProcessingOperationBusinessValidator(KinListOptions options)
    {
        RuleFor(x => x.Type)
            .Must(type =>
                string.Equals(type, "NewList", StringComparison.OrdinalIgnoreCase)
                || string.Equals(type, "AppendItems", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Unsupported audio operation type.")
            .WithErrorCode("invalid_audio_operation_type");

        RuleFor(x => x.ContentType)
            .Must(contentType => HasAllowedContentType(contentType, options.AllowedAudioMimeTypes))
            .WithMessage("Unsupported audio MIME type.")
            .WithErrorCode("invalid_audio_mime_type");

        RuleFor(x => x.DeclaredByteSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(options.MaxAudioBytes)
            .WithMessage("Audio size is outside the configured limits.")
            .WithErrorCode("invalid_audio_size");

        RuleFor(x => x.ListId)
            .NotNull()
            .When(x => string.Equals(x.Type, "AppendItems", StringComparison.OrdinalIgnoreCase))
            .WithMessage("ListId is required for append operations.")
            .WithErrorCode("list_id_required");
    }

    private static bool HasAllowedContentType(string? contentType, string[] allowedMimeTypes)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        var normalizedContentType = contentType.Split(';', 2, StringSplitOptions.TrimEntries)[0].Trim();
        if (string.IsNullOrWhiteSpace(normalizedContentType))
        {
            return false;
        }

        return allowedMimeTypes.Contains(normalizedContentType, StringComparer.OrdinalIgnoreCase);
    }
}
