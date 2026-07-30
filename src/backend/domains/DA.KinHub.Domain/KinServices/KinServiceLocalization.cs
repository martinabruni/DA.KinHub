using DA.KinHub.Domain.Common;

namespace DA.KinHub.Domain.KinServices;

public sealed class KinServiceLocalization
{
    private static readonly HashSet<string> SupportedLanguages = new(StringComparer.Ordinal)
    {
        KinServiceLanguages.It,
        KinServiceLanguages.En
    };

    private KinServiceLocalization()
    {
    }

    private KinServiceLocalization(Guid id, Guid kinServiceId, string language, string name, string description, DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("KinService localization ID is required.");
        }

        if (kinServiceId == Guid.Empty)
        {
            throw new DomainException("KinService localization requires a service ID.");
        }

        Id = id;
        KinServiceId = kinServiceId;
        Language = NormalizeLanguage(language);
        Name = NormalizeRequired(name, "KinService localization name is required.");
        Description = NormalizeRequired(description, "KinService localization description is required.");
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid KinServiceId { get; private set; }

    public string Language { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static KinServiceLocalization Create(Guid id, Guid kinServiceId, string language, string name, string description, DateTimeOffset createdAt)
        => new(id, kinServiceId, language, name, description, createdAt);

    public static string NormalizeLanguage(string? language)
    {
        var normalized = language?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized) || !SupportedLanguages.Contains(normalized))
        {
            throw new DomainException("KinService language is not supported.");
        }

        return normalized;
    }

    private static string NormalizeRequired(string value, string error)
    {
        var normalized = value.Trim();
        if (normalized.Length == 0)
        {
            throw new DomainException(error);
        }

        return normalized;
    }
}
