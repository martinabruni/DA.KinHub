using System.Diagnostics;

namespace Kin.KinHub.KinRecipe.AzureOpenAi.Common;

internal static class OpenAiTelemetry
{
    public const string ActivitySourceName = "Kin.KinHub.KinRecipe.AzureOpenAi";
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}
