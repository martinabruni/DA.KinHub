namespace Kin.KinHub.Shared.Api.Common.Authorization;

public enum FamilyContextOutcome
{
    Success,
    NoFamily,
    Forbidden,
    Unavailable,
}

public sealed record FamilyContextResolution(FamilyContextOutcome Outcome, Guid? FamilyId = null)
{
    public static FamilyContextResolution Success(Guid familyId) => new(FamilyContextOutcome.Success, familyId);
    public static FamilyContextResolution NoFamily() => new(FamilyContextOutcome.NoFamily);
    public static FamilyContextResolution Forbidden() => new(FamilyContextOutcome.Forbidden);
    public static FamilyContextResolution Unavailable() => new(FamilyContextOutcome.Unavailable);
}

public interface IFamilyContextResolver
{
    Task<FamilyContextResolution> ResolveAsync(Guid userId, CancellationToken cancellationToken = default);
}
