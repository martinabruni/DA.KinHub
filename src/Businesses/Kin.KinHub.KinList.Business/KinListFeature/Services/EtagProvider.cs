namespace Kin.KinHub.KinList.Business.KinListFeature;

public sealed class EtagProvider : IEtagProvider
{
    public string ToEtag(Guid version) => $"\"{version:D}\"";

    public bool Matches(string etag, Guid version) =>
        string.Equals(ToEtag(version), etag.Trim(), StringComparison.Ordinal);
}
