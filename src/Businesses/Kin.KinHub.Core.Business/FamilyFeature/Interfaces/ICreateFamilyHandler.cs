namespace Kin.KinHub.Core.Business.FamilyFeature;

public interface ICreateFamilyHandler
{
    Task<Result<CreateFamilyResponse>> HandleAsync(
        CreateFamilyRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);
}
