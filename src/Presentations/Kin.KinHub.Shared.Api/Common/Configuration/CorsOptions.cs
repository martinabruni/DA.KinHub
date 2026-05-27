namespace Kin.KinHub.Shared.Api.Common.Configuration;

public sealed class CorsOptions
{
    public const string SectionName = "Cors";
    public const string PolicyName = "ConfiguredCors";

    public bool AllowAnyOrigin { get; set; } = true;
    public string[] AllowedOrigins { get; set; } = [];
}
