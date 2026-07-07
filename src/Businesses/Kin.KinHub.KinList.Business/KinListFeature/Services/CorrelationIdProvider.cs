using System.Diagnostics;

namespace Kin.KinHub.KinList.Business.KinListFeature;

public interface ICorrelationIdProvider
{
    string? CorrelationId { get; set; }

    // Resolves the effective correlation id: an explicitly set value or the ambient
    // Activity id, falling back to the supplied value and finally a new GUID.
    string Resolve(string? fallback = null);
}

public sealed class CorrelationIdProvider : ICorrelationIdProvider
{
    public string? CorrelationId { get; set; }

    public string Resolve(string? fallback = null) =>
        CorrelationId
        ?? Activity.Current?.Id
        ?? fallback
        ?? Guid.NewGuid().ToString("D");
}
