using System.Diagnostics;

namespace Kin.KinHub.Core.OpenAi.Common;

internal static class OpenAiTelemetry
{
    public const string ActivitySourceName = "Kin.KinHub.Core.OpenAi";
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}
