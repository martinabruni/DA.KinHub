namespace Kin.KinHub.KinList.Business.KinListFeature;

public interface IEtagProvider
{
    string ToEtag(Guid version);
    bool Matches(string etag, Guid version);
}

public sealed class EtagProvider : IEtagProvider
{
    public string ToEtag(Guid version) => $"\"{version:D}\"";

    public bool Matches(string etag, Guid version) =>
        string.Equals(ToEtag(version), etag.Trim(), StringComparison.Ordinal);
}
