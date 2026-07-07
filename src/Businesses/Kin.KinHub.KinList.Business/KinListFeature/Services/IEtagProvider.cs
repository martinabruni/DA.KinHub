namespace Kin.KinHub.KinList.Business.KinListFeature;

public interface IEtagProvider
{
    string ToEtag(Guid version);
    bool Matches(string etag, Guid version);
}
