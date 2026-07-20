using AdvancedFrontier.Domain.Identity;

namespace AdvancedFrontier.Business.Identity;

public sealed record KinListBootstrapResult(string State, Guid? FamilyId)
{
    public static KinListBootstrapResult Family(Guid familyId) => new("family", familyId);

    public static KinListBootstrapResult Onboarding() => new("onboarding", null);
}

public enum FamilyAccessOutcome
{
    Granted = 0,
    ProfileNotFound = 1,
    ProfileInactive = 2,
    MembershipInactiveOrMissing = 3
}

public interface IKinListBootstrapService
{
    Task<KinListBootstrapResult> GetBootstrapAsync(ExternalIdentity externalIdentity, CancellationToken cancellationToken);
}

public interface IFamilyAccessService
{
    Task<FamilyAccessOutcome> CheckAccessAsync(ExternalIdentity externalIdentity, Guid familyId, CancellationToken cancellationToken);
}
