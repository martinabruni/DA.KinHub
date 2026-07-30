using DA.KinHub.Domain.Identity;

namespace DA.KinHub.Business.Identity;

public sealed record KinHubBootstrapResult(string State, Guid? FamilyId)
{
    public static KinHubBootstrapResult Family(Guid familyId) => new("family", familyId);

    public static KinHubBootstrapResult Onboarding() => new("onboarding", null);
}

public enum FamilyAccessOutcome
{
    Granted = 0,
    ProfileNotFound = 1,
    ProfileInactive = 2,
    MembershipInactiveOrMissing = 3
}

public sealed record FamilyAccessResult(FamilyAccessOutcome Outcome, Guid? ApplicationUserId)
{
    public static FamilyAccessResult Granted(Guid applicationUserId) => new(FamilyAccessOutcome.Granted, applicationUserId);

    public static FamilyAccessResult Denied(FamilyAccessOutcome outcome) => new(outcome, null);
}

public interface IKinHubBootstrapService
{
    Task<KinHubBootstrapResult> GetBootstrapAsync(ExternalIdentity externalIdentity, CancellationToken cancellationToken);
}

public interface IFamilyAccessService
{
    Task<FamilyAccessResult> CheckAccessAsync(ExternalIdentity externalIdentity, Guid familyId, CancellationToken cancellationToken);
}

public sealed record FamilyCreationResult(Guid FamilyId, bool Created, bool ReconciledConflict)
{
    public static FamilyCreationResult CreatedFamily(Guid familyId) => new(familyId, true, false);

    public static FamilyCreationResult ExistingFamily(Guid familyId, bool reconciledConflict) => new(familyId, false, reconciledConflict);
}

public interface IFamilyCreationService
{
    Task<FamilyCreationResult> CreateFamilyAsync(ExternalIdentity externalIdentity, string? name, CancellationToken cancellationToken);
}

public sealed record KinHubServiceCatalogItem(string Key, string Route, string Name, string Description);

public sealed record KinHubServiceCatalogResult(IReadOnlyList<KinHubServiceCatalogItem> Services);

public interface IKinHubServiceCatalogService
{
    Task<KinHubServiceCatalogResult> GetCatalogAsync(Guid familyId, string? language, CancellationToken cancellationToken);
}

public interface IKinHubServiceAccessService
{
    Task EnsureAccessAsync(Guid familyId, string serviceKey, CancellationToken cancellationToken);
}
