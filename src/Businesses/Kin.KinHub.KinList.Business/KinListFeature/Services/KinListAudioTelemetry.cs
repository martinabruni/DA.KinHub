using System.Diagnostics;

namespace Kin.KinHub.KinList.Business.KinListFeature;

public static class KinListAudioTelemetry
{
    public const string ActivitySourceName = "Kin.KinHub.KinList.Audio";
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    public static string ResolveCorrelationId(string? fallback = null) =>
        Activity.Current?.Id
        ?? fallback
        ?? Guid.NewGuid().ToString("D");

    public static bool TryParseCorrelationContext(string? correlationId, out ActivityContext context) =>
        ActivityContext.TryParse(correlationId, null, out context);
}
