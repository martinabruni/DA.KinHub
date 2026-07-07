namespace Kin.KinHub.KinRecipe.Api.Common;

public sealed class FamilyContextApiOptions
{
    public const string SectionName = "FamilyContextApi";

    public string BaseUrl { get; set; } = "http://localhost:5001";

    public int TimeoutSeconds { get; set; } = 10;

    public void Validate()
    {
        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException("FamilyContextApi:BaseUrl must be an absolute URL.");
        }

        if (TimeoutSeconds <= 0)
        {
            throw new InvalidOperationException("FamilyContextApi:TimeoutSeconds must be greater than zero.");
        }
    }
}
