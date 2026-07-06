using Kin.KinHub.Core.Business.Common;
using Kin.KinHub.Core.Business.FamilyFeature;
using Kin.KinHub.Core.Domain.FamilyFeature;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kin.KinHub.Core.Test;

/// <summary>
/// Characterization tests for T05b: Kin List registered in the Core service catalog
/// and enabled for every family (new and pre-existing).
/// </summary>
public sealed class KinListCatalogTests
{
    private const int KinConsoleId = (int)KinHubServiceType.KinConsole;
    private const int KinRecipeId = (int)KinHubServiceType.KinRecipe;
    private const int KinListId = (int)KinHubServiceType.KinList;

    private static InMemoryKinHubServiceRepository CreateCatalog() =>
        new(
            new KinHubService
            {
                Id = KinConsoleId,
                Name = "KinConsole",
                BaseUrl = "/kin-console",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new KinHubService
            {
                Id = KinRecipeId,
                Name = "KinRecipe",
                BaseUrl = "/kin-recipe",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new KinHubService
            {
                Id = KinListId,
                Name = "KinList",
                BaseUrl = "/kin-list",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });

    [Fact]
    public async Task Catalog_ContainsKinList_AfterSeeding()
    {
        var catalog = CreateCatalog();

        var service = await catalog.FindByServiceTypeAsync(KinHubServiceType.KinList);

        Assert.NotNull(service);
        Assert.Equal(KinListId, service!.Id);
        Assert.Equal("KinList", service.Name);
        Assert.Equal("/kin-list", service.BaseUrl);
        Assert.True(service.IsActive);
    }

    [Fact]
    public async Task Launcher_Catalog_ExposesKinList_ThroughBusinessService()
    {
        var catalog = CreateCatalog();
        var familyServices = new InMemoryFamilyServiceRepository();
        var families = new InMemoryFamilyRepository();
        var ownership = new FamilyOwnershipService(families, NullLogger<FamilyOwnershipService>.Instance);
        var kinHubService = new KinHubServiceService(catalog, familyServices, ownership);

        var result = await kinHubService.GetAllServicesAsync();

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value!, s => s.Name == "KinList" && s.Id == KinListId && s.IsActive);
    }

    [Fact]
    public async Task CreateFamily_EnablesKinList_ForNewFamily()
    {
        var userId = Guid.NewGuid();
        var families = new InMemoryFamilyRepository();
        var members = new InMemoryFamilyMemberRepository();
        var familyServices = new InMemoryFamilyServiceRepository();
        var catalog = CreateCatalog();

        var handler = new CreateFamilyHandler(families, members, catalog, familyServices, new NoOpCoreTransactionExecutor());

        var result = await handler.HandleAsync(new CreateFamilyRequest
        {
            FamilyName = "Kin Family",
            OwnerProfileName = "Martina",
            AdditionalMembers = [],
        }, userId);

        Assert.True(result.IsSuccess);
        var familyId = result.Value!.FamilyId;

        var kinListAssignment = Assert.Single(
            familyServices.Items.Values,
            assignment => assignment.FamilyId == familyId && assignment.ServiceId == KinListId);
        Assert.True(kinListAssignment.IsActive);
    }

    [Fact]
    public async Task Backfill_AssignsKinList_ToPreExistingFamily_AndIsIdempotent()
    {
        var familyId = Guid.NewGuid();
        var families = new InMemoryFamilyRepository(new Family
        {
            Id = familyId,
            Name = "Legacy Family",
            UserId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });

        // Pre-existing family already has KinConsole + KinRecipe, but not KinList.
        var familyServices = new InMemoryFamilyServiceRepository(
            new FamilyService
            {
                Id = Guid.NewGuid(),
                FamilyId = familyId,
                ServiceId = KinConsoleId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new FamilyService
            {
                Id = Guid.NewGuid(),
                FamilyId = familyId,
                ServiceId = KinRecipeId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });

        // First backfill run: assigns KinList.
        await BackfillKinListAsync(families, familyServices);

        var assignment = Assert.Single(
            familyServices.Items.Values,
            a => a.FamilyId == familyId && a.ServiceId == KinListId);
        Assert.True(assignment.IsActive);
        var assignmentId = assignment.Id;

        // Second backfill run must be a no-op (no duplicate).
        await BackfillKinListAsync(families, familyServices);

        var stillSingle = Assert.Single(
            familyServices.Items.Values,
            a => a.FamilyId == familyId && a.ServiceId == KinListId);
        Assert.Equal(assignmentId, stillSingle.Id);
    }

    /// <summary>
    /// Mirrors the idempotent backfill performed by the AddKinListServiceCatalogEntry
    /// migration: insert a KinList assignment for every family that does not already have one.
    /// </summary>
    private static async Task BackfillKinListAsync(
        InMemoryFamilyRepository families,
        InMemoryFamilyServiceRepository familyServices)
    {
        var now = DateTime.UtcNow;
        foreach (var family in await families.GetAllAsync())
        {
            var existing = await familyServices.FindByFamilyAndServiceAsync(family.Id, KinListId);
            if (existing is not null)
                continue;

            await familyServices.CreateAsync(new FamilyService
            {
                Id = Guid.NewGuid(),
                FamilyId = family.Id,
                ServiceId = KinListId,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }
    }
}
