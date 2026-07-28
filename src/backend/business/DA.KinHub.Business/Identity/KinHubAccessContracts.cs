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

public interface IKinHubBootstrapService
{
    Task<KinHubBootstrapResult> GetBootstrapAsync(ExternalIdentity externalIdentity, CancellationToken cancellationToken);
}

public interface IFamilyAccessService
{
    Task<FamilyAccessOutcome> CheckAccessAsync(ExternalIdentity externalIdentity, Guid familyId, CancellationToken cancellationToken);
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
