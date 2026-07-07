using System.Diagnostics;

namespace Kin.KinHub.KinList.Business.KinListFeature;

public sealed class CorrelationIdProvider : ICorrelationIdProvider
{
    public string? CorrelationId { get; set; }

    public string Resolve(string? fallback = null) =>
        CorrelationId
        ?? Activity.Current?.Id
        ?? fallback
        ?? Guid.NewGuid().ToString("D");
}
