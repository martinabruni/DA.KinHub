using DA.KinHub.Domain.Common;

namespace DA.KinHub.Domain.KinServices;

public sealed class KinService
{
    private KinService()
    {
    }

    private KinService(Guid id, string key, string route, bool isActive, bool isPreconfigured, DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("KinService ID is required.");
        }

        Key = NormalizeRequired(key, "KinService key is required.");
        Route = NormalizeRequired(route, "KinService route is required.");
        Id = id;
        IsActive = isActive;
        IsPreconfigured = isPreconfigured;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Key { get; private set; } = string.Empty;

    public string Route { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public bool IsPreconfigured { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static KinService Create(Guid id, string key, string route, bool isActive, bool isPreconfigured, DateTimeOffset createdAt)
        => new(id, key, route, isActive, isPreconfigured, createdAt);

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
