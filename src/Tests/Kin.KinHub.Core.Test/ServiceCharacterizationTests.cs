using Kin.KinHub.Core.Business.Common;
using Kin.KinHub.Core.Business.FamilyFeature;
using Kin.KinHub.Core.Business.RecipeFeature;
using Kin.KinHub.Core.Domain.RecipeAssistantFeature;
using Kin.KinHub.Core.Domain.FamilyFeature;
using Kin.KinHub.Core.Domain.RecipeFeature;
using Kin.KinHub.Identity.Business.AuthenticationFeature;
using Kin.KinHub.Identity.Domain.AuthenticationFeature;
using Microsoft.Extensions.Logging.Abstractions;
using CoreDuplicateEntityException = Kin.KinHub.Core.Domain.Common.DuplicateEntityException;
using CoreEntityNotFoundException = Kin.KinHub.Core.Domain.Common.EntityNotFoundException;
using IdentityDuplicateEntityException = Kin.KinHub.Identity.Domain.Common.DuplicateEntityException;
using IdentityEntityNotFoundException = Kin.KinHub.Identity.Domain.Common.EntityNotFoundException;

namespace Kin.KinHub.Core.Test;

public sealed class AuthenticationServiceCharacterizationTests
{
    [Fact]
    public async Task RegisterAsync_CreatesUserCredentialAndProvider()
    {
        var users = new InMemoryKinUserRepository();
        var credentials = new InMemoryUserCredentialRepository();
        var providers = new InMemoryUserProviderRepository();
        var refreshTokens = new InMemoryRefreshTokenRepository();
        var service = CreateAuthenticationService(users, credentials, providers, refreshTokens);

        var result = await service.RegisterAsync(new RegisterRequest
        {
            Email = "martina@kinhub.dev",
            Password = "super-secret",
            DisplayName = "Martina",
        });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("martina@kinhub.dev", result.Value!.Email);

        var createdUser = Assert.Single(users.Items.Values);
        Assert.Equal("Martina", createdUser.DisplayName);

        var createdCredential = Assert.Single(credentials.Items.Values);
        Assert.Equal(createdUser.Id, createdCredential.UserId);
        Assert.Equal("hash::super-secret", createdCredential.PasswordHash);

        var createdProvider = Assert.Single(providers.Items.Values);
        Assert.Equal(createdUser.Id, createdProvider.UserId);
        Assert.Equal((int)IdentityProviderType.KinHub, createdProvider.ProviderId);
    }

    [Fact]
    public async Task RefreshTokenAsync_RevokesCurrentToken_AndIssuesReplacement()
    {
        var userId = Guid.NewGuid();
        var users = new InMemoryKinUserRepository(new KinUser
        {
            Id = userId,
            Email = "martina@kinhub.dev",
            DisplayName = "Martina",
            IsEmailVerified = true,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        var storedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = "refresh-current",
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(10),
            Revoked = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        var refreshTokens = new InMemoryRefreshTokenRepository(storedToken);
        var tokenGenerator = new TestTokenGenerator();
        var service = CreateAuthenticationService(
            users,
            new InMemoryUserCredentialRepository(),
            new InMemoryUserProviderRepository(),
            refreshTokens,
            tokenGenerator: tokenGenerator);

        var result = await service.RefreshTokenAsync(storedToken.Token);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(refreshTokens.Items[storedToken.Id].Revoked);
        Assert.Contains(refreshTokens.Items.Values, token => token.Token == "refresh-0" && token.UserId == userId && !token.Revoked);
        Assert.Equal("access::martina@kinhub.dev::1", result.Value!.AccessToken);
        Assert.Equal("refresh-0", result.Value.RefreshToken);
    }

    [Fact]
    public async Task UpdateUserPasswordAsync_ReturnsUnauthorized_WhenCurrentPasswordDoesNotMatch()
    {
        var userId = Guid.NewGuid();
        var service = CreateAuthenticationService(
            new InMemoryKinUserRepository(new KinUser
            {
                Id = userId,
                Email = "martina@kinhub.dev",
                DisplayName = "Martina",
                IsEmailVerified = true,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }),
            new InMemoryUserCredentialRepository(new UserCredential
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PasswordHash = "hash::expected-password",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }),
            new InMemoryUserProviderRepository(),
            new InMemoryRefreshTokenRepository());

        var result = await service.UpdateUserPasswordAsync(userId, new UpdateUserPasswordRequest
        {
            CurrentPassword = "wrong-password",
            NewPassword = "new-password",
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(Kin.KinHub.Identity.Business.Common.ResultStatus.Unauthorized, result.Status);
        Assert.Equal("Invalid current password.", result.Message);
    }

    private static KinHubAuthenticationService CreateAuthenticationService(
        IKinUserRepository userRepository,
        IUserCredentialRepository credentialRepository,
        IUserProviderRepository userProviderRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher? passwordHasher = null,
        ITokenGenerator? tokenGenerator = null)
    {
        passwordHasher ??= new TestPasswordHasher();
        tokenGenerator ??= new TestTokenGenerator();

        var loginResponseFactory = new LoginResponseFactory(tokenGenerator, refreshTokenRepository);

        var providerRegistry = new IdentityProviderRegistry(new IIdentityProvider[]
        {
            new KinHubPasswordIdentityProvider(userRepository, credentialRepository, userProviderRepository, passwordHasher),
        });

        return new KinHubAuthenticationService(
            new RegisterUserHandler(providerRegistry),
            new LoginUserHandler(providerRegistry, loginResponseFactory),
            new RefreshTokenHandler(refreshTokenRepository, userRepository, loginResponseFactory),
            new LogoutUserHandler(refreshTokenRepository),
            new GetCurrentUserHandler(userRepository),
            new UpdateUserEmailHandler(userRepository, credentialRepository, passwordHasher),
            new UpdateUserPasswordHandler(credentialRepository, passwordHasher),
            new DeleteUserHandler(userRepository));
    }
}

public sealed class FamilyServiceCharacterizationTests
{
    [Fact]
    public async Task CreateFamilyAsync_CreatesOwnerMembers_AndEnablesAllServices()
    {
        var userId = Guid.NewGuid();
        var families = new InMemoryFamilyRepository();
        var members = new InMemoryFamilyMemberRepository();
        var familyServices = new InMemoryFamilyServiceRepository();
        var serviceCatalog = new InMemoryKinHubServiceRepository(
            new KinHubService
            {
                Id = 1,
                Name = "Console",
                BaseUrl = "/console",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new KinHubService
            {
                Id = 2,
                Name = "Recipe",
                BaseUrl = "/recipe",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new KinHubService
            {
                Id = 3,
                Name = "KinList",
                BaseUrl = "/kin-list",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
        var service = CreateFamilyService(families, members, serviceCatalog, familyServices);

        var result = await service.CreateFamilyAsync(new CreateFamilyRequest
        {
            FamilyName = "Kin Family",
            OwnerProfileName = "Martina",
            AdditionalMembers = ["Luca", "Giulia"],
        }, userId);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Single(families.Items.Values);
        Assert.Equal(3, members.Items.Count);
        Assert.Contains(members.Items.Values, member => member.Name == "Martina");
        Assert.Contains(members.Items.Values, member => member.Name == "Luca");
        Assert.Contains(members.Items.Values, member => member.Name == "Giulia");
        Assert.Equal(3, familyServices.Items.Count);
        Assert.Contains(familyServices.Items.Values, assignment => assignment.ServiceId == 3);
        Assert.All(familyServices.Items.Values, assignment => Assert.True(assignment.IsActive));
    }

    [Fact]
    public async Task DeleteFamilyMemberAsync_ReturnsConflict_WhenRemovingOnlyMember()
    {
        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var service = CreateFamilyService(
            new InMemoryFamilyRepository(new Family
            {
                Id = familyId,
                Name = "Kin Family",
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }),
            new InMemoryFamilyMemberRepository(new FamilyMember
            {
                Id = memberId,
                Name = "Martina",
                FamilyId = familyId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }),
            new InMemoryKinHubServiceRepository(),
            new InMemoryFamilyServiceRepository());

        var result = await service.DeleteFamilyMemberAsync(familyId, memberId, userId);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Equal("Cannot remove the only member of a family.", result.Message);
    }

    private static KinHubFamilyService CreateFamilyService(
        IFamilyRepository familyRepository,
        IFamilyMemberRepository familyMemberRepository,
        IKinHubServiceRepository kinHubServiceRepository,
        IFamilyServiceRepository familyServiceRepository)
    {
        var ownershipService = new FamilyOwnershipService(familyRepository, NullLogger<FamilyOwnershipService>.Instance);

        return new KinHubFamilyService(
            new CreateFamilyHandler(familyRepository, familyMemberRepository, kinHubServiceRepository, familyServiceRepository),
            new AddFamilyMemberHandler(ownershipService, familyMemberRepository),
            new GetFamilyHandler(ownershipService, familyMemberRepository),
            new DeleteFamilyMemberHandler(ownershipService, familyMemberRepository),
            new UpdateFamilyMemberHandler(ownershipService, familyMemberRepository),
            new UpdateFamilyHandler(ownershipService, familyRepository),
            new DeleteFamilyHandler(ownershipService, familyRepository, familyMemberRepository));
    }
}

public sealed class RecipeServiceCharacterizationTests
{
    [Fact]
    public async Task CreateAsync_ReturnsUnauthorized_WhenRecipeBookBelongsToDifferentFamily()
    {
        var recipeBookId = Guid.NewGuid();
        var userFamily = new Family
        {
            Id = Guid.NewGuid(),
            Name = "Kin Family",
            UserId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        var service = CreateRecipeService(
            new InMemoryRecipeRepository(),
            new InMemoryRecipeBookRepository(new RecipeBook
            {
                Id = recipeBookId,
                Name = "Family Book",
                FamilyId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }),
            new InMemoryRecipeIngredientRepository(),
            new InMemoryRecipeStepRepository(),
            new InMemoryFamilyRepository(userFamily));

        var result = await service.CreateAsync(new CreateRecipeRequest
        {
            Name = "Lasagna",
            Backstory = "Sunday lunch",
            FinalTime = TimeSpan.FromMinutes(90),
            Portions = 6,
            RecipeBookId = recipeBookId,
        }, userFamily.UserId);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Unauthorized, result.Status);
    }

    [Fact]
    public async Task CreateAsync_PersistsInlineIngredientsAndSteps_AndMapsResponse()
    {
        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var recipeRepository = new InMemoryRecipeRepository();
        var ingredientRepository = new InMemoryRecipeIngredientRepository();
        var stepRepository = new InMemoryRecipeStepRepository();
        var service = CreateRecipeService(
            recipeRepository,
            new InMemoryRecipeBookRepository(new RecipeBook
            {
                Id = bookId,
                Name = "Family Book",
                FamilyId = familyId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }),
            ingredientRepository,
            stepRepository,
            new InMemoryFamilyRepository(new Family
            {
                Id = familyId,
                Name = "Kin Family",
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }));

        var result = await service.CreateAsync(new CreateRecipeRequest
        {
            Name = "Lasagna",
            Backstory = "Sunday lunch",
            FinalTime = TimeSpan.FromMinutes(90),
            Portions = 6,
            RecipeBookId = bookId,
            Ingredients =
            [
                new CreateRecipeIngredientInlineRequest
                {
                    Name = "Pasta",
                    MeasureUnit = "g",
                    Quantity = 500,
                },
            ],
            Steps =
            [
                new CreateRecipeStepInlineRequest
                {
                    Order = 1,
                    Description = "Layer the ingredients.",
                },
            ],
        }, userId);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Single(recipeRepository.Items.Values);
        Assert.Single(ingredientRepository.Items.Values);
        Assert.Single(stepRepository.Items.Values);
        Assert.Single(result.Value!.Ingredients);
        Assert.Single(result.Value.Steps);
        Assert.Equal("Pasta", result.Value.Ingredients[0].Name);
        Assert.Equal("Layer the ingredients.", result.Value.Steps[0].Description);
    }

    private static KinHubRecipeService CreateRecipeService(
        IRecipeRepository recipeRepository,
        IRecipeBookRepository recipeBookRepository,
        IRecipeIngredientRepository recipeIngredientRepository,
        IRecipeStepRepository recipeStepRepository,
        IFamilyRepository familyRepository)
    {
        var recipeBookAccessService = new RecipeBookAccessService(familyRepository, recipeBookRepository, NullLogger<RecipeBookAccessService>.Instance);
        var recipeAccessService = new RecipeAccessService(familyRepository, recipeRepository, recipeBookRepository, NullLogger<RecipeAccessService>.Instance);
        var recipeResponseMapper = new RecipeResponseMapper(recipeIngredientRepository, recipeStepRepository);

        return new KinHubRecipeService(
            new CreateRecipeHandler(recipeRepository, recipeIngredientRepository, recipeStepRepository, recipeBookAccessService, recipeResponseMapper),
            new GetRecipesHandler(recipeRepository, recipeBookAccessService, recipeResponseMapper),
            new GetRecipeByIdHandler(recipeAccessService, recipeResponseMapper),
            new UpdateRecipeHandler(recipeRepository, recipeAccessService, recipeResponseMapper),
            new DeleteRecipeHandler(recipeRepository, recipeAccessService));
    }
}

public sealed class RecipeSiblingServiceCharacterizationTests
{
    [Fact]
    public async Task RecipeBook_CreateAsync_UsesCurrentFamily()
    {
        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var service = new KinHubRecipeBookService(
            new CreateRecipeBookHandler(
                new InMemoryRecipeBookRepository(),
                new FamilyOwnershipService(new InMemoryFamilyRepository(new Family
                {
                    Id = familyId,
                    Name = "Kin Family",
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                }), NullLogger<FamilyOwnershipService>.Instance),
                new RecipeBookResponseMapper()),
            new GetRecipeBooksHandler(
                new InMemoryRecipeBookRepository(),
                new FamilyOwnershipService(new InMemoryFamilyRepository(), NullLogger<FamilyOwnershipService>.Instance),
                new RecipeBookResponseMapper()),
            new GetRecipeBookByIdHandler(
                new RecipeBookAccessService(new InMemoryFamilyRepository(), new InMemoryRecipeBookRepository(), NullLogger<RecipeBookAccessService>.Instance),
                new RecipeBookResponseMapper()),
            new UpdateRecipeBookHandler(
                new InMemoryRecipeBookRepository(),
                new RecipeBookAccessService(new InMemoryFamilyRepository(), new InMemoryRecipeBookRepository(), NullLogger<RecipeBookAccessService>.Instance),
                new RecipeBookResponseMapper()),
            new DeleteRecipeBookHandler(
                new InMemoryRecipeBookRepository(),
                new RecipeBookAccessService(new InMemoryFamilyRepository(), new InMemoryRecipeBookRepository(), NullLogger<RecipeBookAccessService>.Instance)));

        var result = await service.CreateAsync(new CreateRecipeBookRequest
        {
            Name = "Family Book",
            Description = "Sunday recipes",
        }, userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(familyId, result.Value!.FamilyId);
    }

    [Fact]
    public async Task RecipeIngredient_UpdateAsync_RegeneratesEmbedding_WhenNameChanges()
    {
        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();
        var ingredientId = Guid.NewGuid();
        var recipeRepository = new InMemoryRecipeRepository(new Recipe
        {
            Id = recipeId,
            Name = "Lasagna",
            FinalTime = TimeSpan.FromMinutes(90),
            Portions = 6,
            RecipeBookId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        var recipeBookRepository = new InMemoryRecipeBookRepository(new RecipeBook
        {
            Id = recipeRepository.Items[recipeId].RecipeBookId,
            Name = "Family Book",
            FamilyId = familyId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        var ingredientRepository = new InMemoryRecipeIngredientRepository(new RecipeIngredient
        {
            Id = ingredientId,
            Name = "Pasta",
            MeasureUnit = "g",
            Quantity = 500,
            RecipeId = recipeId,
            Embedding = [1, 2, 3],
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        var embeddingService = new InMemoryEmbeddingService();
        var service = new KinHubRecipeIngredientService(
            new CreateRecipeIngredientHandler(
                ingredientRepository,
                new RecipeAccessService(new InMemoryFamilyRepository(), recipeRepository, recipeBookRepository, NullLogger<RecipeAccessService>.Instance),
                embeddingService,
                new RecipeIngredientResponseMapper()),
            new GetRecipeIngredientsHandler(
                ingredientRepository,
                new RecipeAccessService(new InMemoryFamilyRepository(), recipeRepository, recipeBookRepository, NullLogger<RecipeAccessService>.Instance),
                new RecipeIngredientResponseMapper()),
            new GetRecipeIngredientByIdHandler(
                new RecipeIngredientAccessService(
                    new InMemoryFamilyRepository(new Family
                    {
                        Id = familyId,
                        Name = "Kin Family",
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                    }),
                    ingredientRepository,
                    recipeRepository,
                    recipeBookRepository,
                    NullLogger<RecipeIngredientAccessService>.Instance),
                new RecipeIngredientResponseMapper()),
            new UpdateRecipeIngredientHandler(
                ingredientRepository,
                new RecipeIngredientAccessService(
                    new InMemoryFamilyRepository(new Family
                    {
                        Id = familyId,
                        Name = "Kin Family",
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                    }),
                    ingredientRepository,
                    recipeRepository,
                    recipeBookRepository,
                    NullLogger<RecipeIngredientAccessService>.Instance),
                embeddingService,
                new RecipeIngredientResponseMapper()),
            new DeleteRecipeIngredientHandler(
                ingredientRepository,
                new RecipeIngredientAccessService(
                    new InMemoryFamilyRepository(new Family
                    {
                        Id = familyId,
                        Name = "Kin Family",
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                    }),
                    ingredientRepository,
                    recipeRepository,
                    recipeBookRepository,
                    NullLogger<RecipeIngredientAccessService>.Instance)));

        var result = await service.UpdateAsync(ingredientId, new UpdateRecipeIngredientRequest
        {
            Name = "Fresh Pasta",
            MeasureUnit = "g",
            Quantity = 750,
        }, userId);

        Assert.True(result.IsSuccess);
        Assert.Equal("Fresh Pasta", ingredientRepository.Items[ingredientId].Name);
        Assert.Equal([11f], ingredientRepository.Items[ingredientId].Embedding);
    }

    [Fact]
    public async Task RecipeStep_DeleteAsync_SoftDeletesAccessibleStep()
    {
        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var recipeBookId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var stepRepository = new InMemoryRecipeStepRepository(new RecipeStep
        {
            Id = stepId,
            Order = 1,
            Description = "Layer ingredients",
            RecipeId = recipeId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        var service = new KinHubRecipeStepService(
            new CreateRecipeStepHandler(
                stepRepository,
                new RecipeAccessService(new InMemoryFamilyRepository(), new InMemoryRecipeRepository(), new InMemoryRecipeBookRepository(), NullLogger<RecipeAccessService>.Instance),
                new RecipeStepResponseMapper()),
            new GetRecipeStepsHandler(
                stepRepository,
                new RecipeAccessService(new InMemoryFamilyRepository(), new InMemoryRecipeRepository(), new InMemoryRecipeBookRepository(), NullLogger<RecipeAccessService>.Instance),
                new RecipeStepResponseMapper()),
            new GetRecipeStepByIdHandler(
                new RecipeStepAccessService(
                    new InMemoryFamilyRepository(new Family
                    {
                        Id = familyId,
                        Name = "Kin Family",
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                    }),
                    stepRepository,
                    new InMemoryRecipeRepository(new Recipe
                    {
                        Id = recipeId,
                        Name = "Lasagna",
                        FinalTime = TimeSpan.FromMinutes(90),
                        Portions = 6,
                        RecipeBookId = recipeBookId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                    }),
                    new InMemoryRecipeBookRepository(new RecipeBook
                    {
                        Id = recipeBookId,
                        Name = "Family Book",
                        FamilyId = familyId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                    }),
                    NullLogger<RecipeStepAccessService>.Instance),
                new RecipeStepResponseMapper()),
            new UpdateRecipeStepHandler(
                stepRepository,
                new RecipeStepAccessService(
                    new InMemoryFamilyRepository(new Family
                    {
                        Id = familyId,
                        Name = "Kin Family",
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                    }),
                    stepRepository,
                    new InMemoryRecipeRepository(new Recipe
                    {
                        Id = recipeId,
                        Name = "Lasagna",
                        FinalTime = TimeSpan.FromMinutes(90),
                        Portions = 6,
                        RecipeBookId = recipeBookId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                    }),
                    new InMemoryRecipeBookRepository(new RecipeBook
                    {
                        Id = recipeBookId,
                        Name = "Family Book",
                        FamilyId = familyId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                    }),
                    NullLogger<RecipeStepAccessService>.Instance),
                new RecipeStepResponseMapper()),
            new DeleteRecipeStepHandler(
                stepRepository,
                new RecipeStepAccessService(
                    new InMemoryFamilyRepository(new Family
                    {
                        Id = familyId,
                        Name = "Kin Family",
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                    }),
                    stepRepository,
                    new InMemoryRecipeRepository(new Recipe
                    {
                        Id = recipeId,
                        Name = "Lasagna",
                        FinalTime = TimeSpan.FromMinutes(90),
                        Portions = 6,
                        RecipeBookId = recipeBookId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                    }),
                    new InMemoryRecipeBookRepository(new RecipeBook
                    {
                        Id = recipeBookId,
                        Name = "Family Book",
                        FamilyId = familyId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                    }),
                    NullLogger<RecipeStepAccessService>.Instance)));

        var result = await service.DeleteAsync(stepId, userId);

        Assert.True(result.IsSuccess);
        Assert.True(stepRepository.Items[stepId].IsDeleted);
    }
}

internal sealed class TestPasswordHasher : IPasswordHasher
{
    public string Hash(string plainPassword) => $"hash::{plainPassword}";

    public bool Verify(string plainPassword, string hashedPassword) =>
        hashedPassword == Hash(plainPassword);
}

internal sealed class TestTokenGenerator : ITokenGenerator
{
    private int _accessTokenCounter;
    private int _refreshTokenCounter;

    public int AccessTokenExpirySeconds => 3600;

    public string GenerateAccessToken(KinUser user, IReadOnlyList<string> roles, IReadOnlyList<string>? scopes = null) =>
        $"access::{user.Email}::{++_accessTokenCounter}";

    public string GenerateRefreshToken() => $"refresh-{_refreshTokenCounter++}";
}

internal sealed class InMemoryEmbeddingService : IEmbeddingService
{
    public Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default) =>
        Task.FromResult(new[] { (float)text.Length });
}

internal sealed class InMemoryKinUserRepository : IKinUserRepository
{
    public Dictionary<Guid, KinUser> Items { get; } = [];

    public InMemoryKinUserRepository(params KinUser[] users)
    {
        foreach (var user in users)
            Items[user.Id] = user;
    }

    public Task<KinUser> CreateAsync(KinUser model)
    {
        if (Items.Values.Any(user => string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase)))
            throw new IdentityDuplicateEntityException(nameof(KinUser), nameof(KinUser.Email), model.Email);

        Items[model.Id] = model;
        return Task.FromResult(model);
    }

    public Task<KinUser> DeleteAsync(Guid key)
    {
        var user = GetExisting(key);
        Items.Remove(key);
        return Task.FromResult(user);
    }

    public Task<KinUser?> FindByEmailAsync(string email) =>
        Task.FromResult(Items.Values.SingleOrDefault(user => string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase)));

    public Task<KinUser> GetAsync(Guid key) => Task.FromResult(GetExisting(key));

    public Task<KinUser> UpdateAsync(Guid key, KinUser model)
    {
        GetExisting(key);
        Items[key] = model;
        return Task.FromResult(model);
    }

    private KinUser GetExisting(Guid key) =>
        Items.TryGetValue(key, out var user)
            ? user
            : throw new IdentityEntityNotFoundException(nameof(KinUser), key);
}

internal sealed class InMemoryUserCredentialRepository : IUserCredentialRepository
{
    public Dictionary<Guid, UserCredential> Items { get; } = [];

    public InMemoryUserCredentialRepository(params UserCredential[] credentials)
    {
        foreach (var credential in credentials)
            Items[credential.Id] = credential;
    }

    public Task<UserCredential> CreateAsync(UserCredential model)
    {
        Items[model.Id] = model;
        return Task.FromResult(model);
    }

    public Task<UserCredential> DeleteAsync(Guid key)
    {
        var credential = GetAsync(key).Result;
        Items.Remove(key);
        return Task.FromResult(credential);
    }

    public Task<UserCredential?> GetByUserIdAsync(Guid userId) =>
        Task.FromResult(Items.Values.SingleOrDefault(credential => credential.UserId == userId));

    public Task<UserCredential> GetAsync(Guid key)
    {
        if (Items.TryGetValue(key, out var credential))
            return Task.FromResult(credential);

        throw new IdentityEntityNotFoundException(nameof(UserCredential), key);
    }

    public Task<UserCredential> UpdateAsync(Guid key, UserCredential model)
    {
        _ = GetAsync(key);
        Items[key] = model;
        return Task.FromResult(model);
    }
}

internal sealed class InMemoryUserProviderRepository : IUserProviderRepository
{
    public Dictionary<Guid, UserProvider> Items { get; } = [];

    public InMemoryUserProviderRepository(params UserProvider[] providers)
    {
        foreach (var provider in providers)
            Items[provider.Id] = provider;
    }

    public Task<UserProvider> CreateAsync(UserProvider model)
    {
        Items[model.Id] = model;
        return Task.FromResult(model);
    }

    public Task<UserProvider> DeleteAsync(Guid key)
    {
        var provider = GetAsync(key).Result;
        Items.Remove(key);
        return Task.FromResult(provider);
    }

    public Task<UserProvider> GetAsync(Guid key)
    {
        if (Items.TryGetValue(key, out var provider))
            return Task.FromResult(provider);

        throw new IdentityEntityNotFoundException(nameof(UserProvider), key);
    }

    public Task<UserProvider> UpdateAsync(Guid key, UserProvider model)
    {
        _ = GetAsync(key);
        Items[key] = model;
        return Task.FromResult(model);
    }

    public Task<IReadOnlyList<UserProvider>> GetByUserIdAsync(Guid userId)
    {
        IReadOnlyList<UserProvider> matches = Items.Values
            .Where(x => x.UserId == userId)
            .ToList();
        return Task.FromResult(matches);
    }

    public Task<UserProvider?> GetByUserAndProviderAsync(Guid userId, int providerId)
    {
        var match = Items.Values.FirstOrDefault(x => x.UserId == userId && x.ProviderId == providerId);
        return Task.FromResult(match);
    }
}

internal sealed class InMemoryRefreshTokenRepository : IRefreshTokenRepository
{
    public Dictionary<Guid, RefreshToken> Items { get; } = [];

    public InMemoryRefreshTokenRepository(params RefreshToken[] refreshTokens)
    {
        foreach (var refreshToken in refreshTokens)
            Items[refreshToken.Id] = refreshToken;
    }

    public Task<RefreshToken> CreateAsync(RefreshToken model)
    {
        Items[model.Id] = model;
        return Task.FromResult(model);
    }

    public Task<RefreshToken> DeleteAsync(Guid key)
    {
        var refreshToken = GetAsync(key).Result;
        Items.Remove(key);
        return Task.FromResult(refreshToken);
    }

    public Task<RefreshToken?> FindByTokenAsync(string token) =>
        Task.FromResult(Items.Values.SingleOrDefault(refreshToken => refreshToken.Token == token));

    public Task<RefreshToken> GetAsync(Guid key)
    {
        if (Items.TryGetValue(key, out var refreshToken))
            return Task.FromResult(refreshToken);

        throw new IdentityEntityNotFoundException(nameof(RefreshToken), key);
    }

    public Task RevokeAllByUserIdAsync(Guid userId)
    {
        foreach (var refreshToken in Items.Values.Where(token => token.UserId == userId && !token.Revoked))
            refreshToken.Revoked = true;

        return Task.CompletedTask;
    }

    public Task<RefreshToken> UpdateAsync(Guid key, RefreshToken model)
    {
        _ = GetAsync(key);
        Items[key] = model;
        return Task.FromResult(model);
    }
}

internal sealed class InMemoryFamilyRepository : IFamilyRepository
{
    public Dictionary<Guid, Family> Items { get; } = [];

    public InMemoryFamilyRepository(params Family[] families)
    {
        foreach (var family in families)
            Items[family.Id] = family;
    }

    public Task<Family> CreateAsync(Family model)
    {
        Items[model.Id] = model;
        return Task.FromResult(model);
    }

    public Task<Family> DeleteAsync(Guid key)
    {
        var family = GetExisting(key);
        Items.Remove(key);
        return Task.FromResult(family);
    }

    public Task<Family?> FindByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Values.SingleOrDefault(family => family.UserId == userId && !family.IsDeleted));

    public Task<IReadOnlyList<Family>> GetAllAsync() =>
        Task.FromResult<IReadOnlyList<Family>>(Items.Values.ToList());

    public Task<Family> GetAsync(Guid key) => Task.FromResult(GetExisting(key));

    public Task<Family> UpdateAsync(Guid key, Family model)
    {
        GetExisting(key);
        Items[key] = model;
        return Task.FromResult(model);
    }

    private Family GetExisting(Guid key) =>
        Items.TryGetValue(key, out var family)
            ? family
            : throw new CoreEntityNotFoundException(nameof(Family), key);
}

internal sealed class InMemoryFamilyMemberRepository : IFamilyMemberRepository
{
    public Dictionary<Guid, FamilyMember> Items { get; } = [];

    public InMemoryFamilyMemberRepository(params FamilyMember[] members)
    {
        foreach (var member in members)
            Items[member.Id] = member;
    }

    public Task<FamilyMember> CreateAsync(FamilyMember model)
    {
        if (Items.Values.Any(member =>
                member.FamilyId == model.FamilyId
                && !member.IsDeleted
                && string.Equals(member.Name, model.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new CoreDuplicateEntityException(nameof(FamilyMember), nameof(FamilyMember.Name), model.Name);
        }

        Items[model.Id] = model;
        return Task.FromResult(model);
    }

    public Task<FamilyMember> DeleteAsync(Guid key)
    {
        var member = GetExisting(key);
        Items.Remove(key);
        return Task.FromResult(member);
    }

    public Task<FamilyMember?> FindByNameAsync(Guid familyId, string name, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Values.SingleOrDefault(member =>
            member.FamilyId == familyId
            && !member.IsDeleted
            && string.Equals(member.Name, name, StringComparison.OrdinalIgnoreCase)));

    public Task<IReadOnlyList<FamilyMember>> GetAllAsync() =>
        Task.FromResult<IReadOnlyList<FamilyMember>>(Items.Values.ToList());

    public Task<FamilyMember> GetAsync(Guid key) => Task.FromResult(GetExisting(key));

    public Task<IReadOnlyList<FamilyMember>> GetByFamilyIdAsync(Guid familyId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<FamilyMember>>(Items.Values.Where(member => member.FamilyId == familyId && !member.IsDeleted).ToList());

    public Task<FamilyMember> UpdateAsync(Guid key, FamilyMember model)
    {
        GetExisting(key);
        Items[key] = model;
        return Task.FromResult(model);
    }

    private FamilyMember GetExisting(Guid key) =>
        Items.TryGetValue(key, out var member)
            ? member
            : throw new CoreEntityNotFoundException(nameof(FamilyMember), key);
}

internal sealed class InMemoryKinHubServiceRepository : IKinHubServiceRepository
{
    public Dictionary<int, KinHubService> Items { get; } = [];

    public InMemoryKinHubServiceRepository(params KinHubService[] services)
    {
        foreach (var service in services)
            Items[service.Id] = service;
    }

    public Task<KinHubService> CreateAsync(KinHubService model)
    {
        Items[model.Id] = model;
        return Task.FromResult(model);
    }

    public Task<KinHubService> DeleteAsync(int key)
    {
        var service = GetExisting(key);
        Items.Remove(key);
        return Task.FromResult(service);
    }

    public Task<KinHubService?> FindByServiceTypeAsync(KinHubServiceType serviceType, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Values.SingleOrDefault(service => service.Id == (int)serviceType));

    public Task<IReadOnlyList<KinHubService>> GetAllAsync() =>
        Task.FromResult<IReadOnlyList<KinHubService>>(Items.Values.Where(service => service.IsActive).ToList());

    public Task<KinHubService> GetAsync(int key) => Task.FromResult(GetExisting(key));

    public Task<KinHubService> UpdateAsync(int key, KinHubService model)
    {
        GetExisting(key);
        Items[key] = model;
        return Task.FromResult(model);
    }

    private KinHubService GetExisting(int key) =>
        Items.TryGetValue(key, out var service)
            ? service
            : throw new CoreEntityNotFoundException(nameof(KinHubService), key);
}

internal sealed class InMemoryFamilyServiceRepository : IFamilyServiceRepository
{
    public Dictionary<Guid, FamilyService> Items { get; } = [];

    public InMemoryFamilyServiceRepository(params FamilyService[] assignments)
    {
        foreach (var assignment in assignments)
            Items[assignment.Id] = assignment;
    }

    public Task<FamilyService> CreateAsync(FamilyService model)
    {
        Items[model.Id] = model;
        return Task.FromResult(model);
    }

    public Task<FamilyService> DeleteAsync(Guid key)
    {
        var assignment = GetExisting(key);
        Items.Remove(key);
        return Task.FromResult(assignment);
    }

    public Task<FamilyService?> FindByFamilyAndServiceAsync(Guid familyId, int serviceId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Values.SingleOrDefault(assignment => assignment.FamilyId == familyId && assignment.ServiceId == serviceId));

    public Task<IReadOnlyList<FamilyService>> GetAllAsync() =>
        Task.FromResult<IReadOnlyList<FamilyService>>(Items.Values.ToList());

    public Task<FamilyService> GetAsync(Guid key) => Task.FromResult(GetExisting(key));

    public Task<IReadOnlyList<FamilyService>> GetByFamilyIdAsync(Guid familyId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<FamilyService>>(Items.Values.Where(assignment => assignment.FamilyId == familyId && assignment.IsActive).ToList());

    public Task<FamilyService> UpdateAsync(Guid key, FamilyService model)
    {
        GetExisting(key);
        Items[key] = model;
        return Task.FromResult(model);
    }

    private FamilyService GetExisting(Guid key) =>
        Items.TryGetValue(key, out var assignment)
            ? assignment
            : throw new CoreEntityNotFoundException(nameof(FamilyService), key);
}

internal sealed class InMemoryRecipeRepository : IRecipeRepository
{
    public Dictionary<Guid, Recipe> Items { get; } = [];

    public InMemoryRecipeRepository(params Recipe[] recipes)
    {
        foreach (var recipe in recipes)
            Items[recipe.Id] = recipe;
    }

    public Task<Recipe> AddAsync(Recipe recipe, CancellationToken cancellationToken = default)
    {
        Items[recipe.Id] = recipe;
        return Task.FromResult(recipe);
    }

    public Task<IReadOnlyList<Recipe>> GetAllByFamilyIdAsync(Guid recipeBookId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Recipe>>(Items.Values.Where(recipe => recipe.RecipeBookId == recipeBookId && !recipe.IsDeleted).ToList());

    public Task<Recipe?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.TryGetValue(id, out var recipe) && !recipe.IsDeleted ? recipe : null);

    public Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (Items.TryGetValue(id, out var recipe))
        {
            recipe.IsDeleted = true;
        }

        return Task.CompletedTask;
    }

    public Task<Recipe> UpdateAsync(Recipe recipe, CancellationToken cancellationToken = default)
    {
        Items[recipe.Id] = recipe;
        return Task.FromResult(recipe);
    }
}

internal sealed class InMemoryRecipeBookRepository : IRecipeBookRepository
{
    public Dictionary<Guid, RecipeBook> Items { get; } = [];

    public InMemoryRecipeBookRepository(params RecipeBook[] recipeBooks)
    {
        foreach (var recipeBook in recipeBooks)
            Items[recipeBook.Id] = recipeBook;
    }

    public Task<RecipeBook> AddAsync(RecipeBook recipeBook, CancellationToken cancellationToken = default)
    {
        Items[recipeBook.Id] = recipeBook;
        return Task.FromResult(recipeBook);
    }

    public Task<IReadOnlyList<RecipeBook>> GetAllByFamilyIdAsync(Guid familyId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<RecipeBook>>(Items.Values.Where(book => book.FamilyId == familyId && !book.IsDeleted).ToList());

    public Task<RecipeBook?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.TryGetValue(id, out var recipeBook) && !recipeBook.IsDeleted ? recipeBook : null);

    public Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (Items.TryGetValue(id, out var recipeBook))
        {
            recipeBook.IsDeleted = true;
        }

        return Task.CompletedTask;
    }

    public Task<RecipeBook> UpdateAsync(RecipeBook recipeBook, CancellationToken cancellationToken = default)
    {
        Items[recipeBook.Id] = recipeBook;
        return Task.FromResult(recipeBook);
    }
}

internal sealed class InMemoryRecipeIngredientRepository : IRecipeIngredientRepository
{
    public Dictionary<Guid, RecipeIngredient> Items { get; } = [];

    public InMemoryRecipeIngredientRepository(params RecipeIngredient[] ingredients)
    {
        foreach (var ingredient in ingredients)
            Items[ingredient.Id] = ingredient;
    }

    public Task<RecipeIngredient> AddAsync(RecipeIngredient ingredient, CancellationToken cancellationToken = default)
    {
        Items[ingredient.Id] = ingredient;
        return Task.FromResult(ingredient);
    }

    public Task<IReadOnlyList<RecipeIngredient>> GetAllByFamilyIdAsync(Guid recipeId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<RecipeIngredient>>(Items.Values.Where(ingredient => ingredient.RecipeId == recipeId).ToList());

    public Task<RecipeIngredient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.TryGetValue(id, out var ingredient) ? ingredient : null);

    public Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Items.Remove(id);
        return Task.CompletedTask;
    }

    public Task<RecipeIngredient> UpdateAsync(RecipeIngredient ingredient, CancellationToken cancellationToken = default)
    {
        Items[ingredient.Id] = ingredient;
        return Task.FromResult(ingredient);
    }
}

internal sealed class InMemoryRecipeStepRepository : IRecipeStepRepository
{
    public Dictionary<Guid, RecipeStep> Items { get; } = [];

    public InMemoryRecipeStepRepository(params RecipeStep[] steps)
    {
        foreach (var step in steps)
            Items[step.Id] = step;
    }

    public Task<RecipeStep> AddAsync(RecipeStep step, CancellationToken cancellationToken = default)
    {
        Items[step.Id] = step;
        return Task.FromResult(step);
    }

    public Task<IReadOnlyList<RecipeStep>> GetAllByFamilyIdAsync(Guid recipeId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<RecipeStep>>(Items.Values.Where(step => step.RecipeId == recipeId && !step.IsDeleted).ToList());

    public Task<RecipeStep?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.TryGetValue(id, out var step) && !step.IsDeleted ? step : null);

    public Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (Items.TryGetValue(id, out var step))
        {
            step.IsDeleted = true;
        }

        return Task.CompletedTask;
    }

    public Task<RecipeStep> UpdateAsync(RecipeStep step, CancellationToken cancellationToken = default)
    {
        Items[step.Id] = step;
        return Task.FromResult(step);
    }
}
