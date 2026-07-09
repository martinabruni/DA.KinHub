namespace Kin.KinHub.Identity.Api.Common.Configuration;

public sealed class CorsOptions
{
    public const string SectionName = "Cors";
    public const string PolicyName = "ConfiguredCors";

    public bool AllowAnyOrigin { get; set; } = false;
    public string[] AllowedOrigins { get; set; } = [];
}
