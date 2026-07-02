namespace Kin.KinHub.KinList.Api.Common.Configuration;

public sealed class CoreApiOptions
{
    public const string SectionName = "CoreApi";

    public string BaseUrl { get; set; } = "http://localhost:5000";
    public int TimeoutSeconds { get; set; } = 10;

    public void Validate()
    {
        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException("CoreApi:BaseUrl must be an absolute URL.");
        }

        if (TimeoutSeconds <= 0)
        {
            throw new InvalidOperationException("CoreApi:TimeoutSeconds must be greater than zero.");
        }
    }
}
