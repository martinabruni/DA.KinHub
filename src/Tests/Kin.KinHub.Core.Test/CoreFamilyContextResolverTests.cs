using Kin.KinHub.App.Functions.Common;
using Kin.KinHub.App.Functions.Common.Authorization;
using Kin.KinHub.Core.Business.FamilyFeature;
using Kin.KinHub.Core.Domain.FamilyFeature;

namespace Kin.KinHub.Core.Test;

public sealed class CoreFamilyContextResolverTests
{
    [Fact]
    public async Task ResolveAsync_WhenOwnershipServiceSucceeds_ReturnsSuccess()
    {
        var familyId = Guid.NewGuid();
        var family = new Family { Id = familyId, Name = "Test", UserId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var resolver = new CoreFamilyContextResolver(new FakeFamilyOwnershipService(FamilyAccessResult.Success(family)));

        var result = await resolver.ResolveAsync(Guid.NewGuid());

        Assert.Equal(FamilyContextOutcome.Success, result.Outcome);
        Assert.Equal(familyId, result.FamilyId);
    }

    [Fact]
    public async Task ResolveAsync_WhenOwnershipServiceReturnsNotFound_ReturnsNoFamily()
    {
        var resolver = new CoreFamilyContextResolver(new FakeFamilyOwnershipService(FamilyAccessResult.NotFound("no family")));

        var result = await resolver.ResolveAsync(Guid.NewGuid());

        Assert.Equal(FamilyContextOutcome.NoFamily, result.Outcome);
    }

    [Fact]
    public async Task ResolveAsync_WhenOwnershipServiceReturnsUnauthorized_ReturnsForbidden()
    {
        var resolver = new CoreFamilyContextResolver(new FakeFamilyOwnershipService(FamilyAccessResult.Unauthorized("nope")));

        var result = await resolver.ResolveAsync(Guid.NewGuid());

        Assert.Equal(FamilyContextOutcome.Forbidden, result.Outcome);
    }

    [Fact]
    public async Task ResolveAsync_WhenOwnershipServiceUnavailable_ReturnsUnavailable()
    {
        var resolver = new CoreFamilyContextResolver(new FakeFamilyOwnershipService(FamilyAccessResult.ServiceUnavailable("down")));

        var result = await resolver.ResolveAsync(Guid.NewGuid());

        Assert.Equal(FamilyContextOutcome.Unavailable, result.Outcome);
    }

    private sealed class FakeFamilyOwnershipService : IFamilyOwnershipService
    {
        private readonly FamilyAccessResult _result;

        public FakeFamilyOwnershipService(FamilyAccessResult result) => _result = result;

        public Task<FamilyAccessResult> GetCurrentFamilyAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_result);

        public Task<FamilyAccessResult> EnsureOwnershipAsync(Guid familyId, Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_result);
    }
}
