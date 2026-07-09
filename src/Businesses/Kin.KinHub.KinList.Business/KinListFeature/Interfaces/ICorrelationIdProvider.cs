namespace Kin.KinHub.KinList.Business.KinListFeature;

public interface ICorrelationIdProvider
{
    string? CorrelationId { get; set; }
    string Resolve(string? fallback = null);
}
