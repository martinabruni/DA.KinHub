extern alias KinListApi;

using Kin.KinHub.Core.Business.FamilyFeature;
using Kin.KinHub.Identity.Domain.Common;
using Kin.KinHub.KinList.Business.Common;
using Kin.KinHub.KinList.Business.KinListFeature;
using Kin.KinHub.KinList.Domain.KinListFeature;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using KinListApiProgram = KinListApi::Program;
using DomainKinList = Kin.KinHub.KinList.Domain.KinListFeature.KinList;
using DomainKinListItem = Kin.KinHub.KinList.Domain.KinListFeature.KinListItem;

namespace Kin.KinHub.Core.Test;

/// <summary>
/// Boots the real KinList API host (controllers, validators, mappers, middleware, options
/// validation) while swapping only the outer dependencies (persistence, authenticated user,
/// family ownership, audio generator) for deterministic in-memory fakes. This exercises the
/// full HTTP pipeline without requiring PostgreSQL or Azure.
/// </summary>
public sealed class KinListApiFactory : WebApplicationFactory<KinListApiProgram>
{
    private const string TestJwtSecret = "integration-only-kinhub-jwt-secret-000000000001";
    public static readonly Guid FamilyA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid FamilyB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public static readonly Guid UserId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    public InMemoryKinListStore Store { get; } = new();
    public MutableCurrentUser CurrentUser { get; } = new()
    {
        UserId = UserId,
        Email = "integration@kinhub.dev",
        IsAuthenticated = true,
        FamilyId = FamilyA,
        HasFamilyContext = true,
    };

    public ConfigurableAudioDraftGenerator AudioGenerator { get; } = new();

    public new HttpClient CreateClient()
    {
        var client = base.CreateClient();
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtSecret)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "http://localhost",
            audience: "kinhub.api",
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, "integration@kinhub.dev"),
                new Claim("scope", "kinhub.api"),
            ],
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: credentials);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            new JwtSecurityTokenHandler().WriteToken(token));
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // The KinList API host reads its own configuration during Program.Main (before the test
        // host's ConfigureAppConfiguration is layered in), including the KINHUB_-prefixed
        // environment variables. Setting them here guarantees the connection string and secrets
        // are available regardless of configuration-source ordering. No real PostgreSQL/Azure
        // connection is ever opened because the repositories are replaced below.
        Environment.SetEnvironmentVariable("KINHUB_ConnectionStrings__KinHub", "Host=localhost;Database=kinhub;Username=kinhub;Password=kinhub");
        Environment.SetEnvironmentVariable("KINHUB_Jwt__Issuer", "http://localhost");
        Environment.SetEnvironmentVariable("KINHUB_Jwt__Secret", TestJwtSecret);
        Environment.SetEnvironmentVariable("KINHUB_FamilyContextApi__BaseUrl", "http://localhost:5001");

        builder.ConfigureAppConfiguration(configuration =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:KinHub"] = "Host=localhost;Database=kinhub;Username=kinhub;Password=kinhub",
                ["Jwt:Issuer"] = "http://localhost",
                ["Jwt:Secret"] = TestJwtSecret,
                ["Jwt:Audience"] = "kinhub.api",
                ["FamilyContextApi:BaseUrl"] = "http://localhost:5001",
            });
        });

        // The test host defaults to eager scope/build validation in Development. Production does not
        // build-validate, and the KinList controllers under test never resolve the Core.Business
        // family/recipe handlers (those live behind other APIs). Match production semantics so the
        // host boots without trying to build unrelated graphs whose PostgreSQL repositories are not
        // registered in this API.
        builder.UseDefaultServiceProvider(options =>
        {
            options.ValidateOnBuild = false;
            options.ValidateScopes = false;
        });

        builder.ConfigureTestServices(services =>
        {
            // Persistence: replace the EF repositories + transaction executor with in-memory fakes
            // so the host never touches PostgreSQL, but all service logic runs unchanged.
            services.RemoveAll<IKinListRepository>();
            services.RemoveAll<IKinListItemRepository>();
            services.RemoveAll<IIdempotencyRecordRepository>();
            services.RemoveAll<IKinListTransactionExecutor>();
            services.AddSingleton(Store);
            services.AddSingleton<IKinListRepository>(Store);
            services.AddSingleton<IKinListItemRepository>(Store);
            services.AddSingleton<IIdempotencyRecordRepository>(Store);
            services.AddScoped<IKinListTransactionExecutor, TestKinListTransactionExecutor>();

            // Authenticated user + family ownership: no real JWT is sent, so the middleware
            // no-ops; the controller reads our controllable current user directly.
            services.RemoveAll<ICurrentUser>();
            services.AddScoped<ICurrentUser>(_ => CurrentUser);
            services.RemoveAll<IFamilyOwnershipService>();
            services.AddSingleton<IFamilyOwnershipService>(new StubFamilyOwnershipService());

            // Audio generator: deterministic fake (T03 covers audio behavior in isolation).
            services.RemoveAll<IKinListAudioDraftGenerator>();
            services.AddSingleton<IKinListAudioDraftGenerator>(AudioGenerator);
        });
    }
}

/// <summary>Mutable <see cref="ICurrentUser"/> so a test can flip family/user context per call.</summary>
public sealed class MutableCurrentUser : ICurrentUser
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public IReadOnlyList<string> Roles { get; set; } = [];
    public bool IsAuthenticated { get; set; }
    public Guid FamilyId { get; set; }
    public bool HasFamilyContext { get; set; }
}

internal sealed class StubFamilyOwnershipService : IFamilyOwnershipService
{
    public Task<FamilyAccessResult> GetCurrentFamilyAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(FamilyAccessResult.NotFound("Family resolution is bypassed in integration tests."));

    public Task<FamilyAccessResult> EnsureOwnershipAsync(Guid familyId, Guid userId, CancellationToken cancellationToken = default) =>
        GetCurrentFamilyAsync(userId, cancellationToken);
}

/// <summary>Configurable audio draft generator whose parse result each test controls.</summary>
public sealed class ConfigurableAudioDraftGenerator : IKinListAudioDraftGenerator
{
    public Result<ParsedKinListAudioDraft> Result { get; set; } =
        Kin.KinHub.KinList.Business.Common.Result<ParsedKinListAudioDraft>.Success(new ParsedKinListAudioDraft
        {
            Title = "Spesa",
            Items = ["Latte", "Pane"],
            DetectedLanguage = "it-IT",
            PromptVersion = "kinlist-audio-v1",
        });

    public Task<Result<ParsedKinListAudioDraft>> ParseAsync(KinListAudioCommand command, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result);
}

/// <summary>
/// Thread-safe in-memory implementation of all three KinList repositories. Clones on read/write
/// so callers cannot mutate stored state by reference (mirrors EF's detached behavior).
/// </summary>
public sealed class InMemoryKinListStore : IKinListRepository, IKinListItemRepository, IIdempotencyRecordRepository
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, DomainKinList> _lists = [];
    private readonly Dictionary<Guid, DomainKinListItem> _items = [];
    private readonly List<IdempotencyRecord> _records = [];

    public Task<IReadOnlyList<DomainKinList>> GetAllByFamilyIdAsync(Guid familyId, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<DomainKinList>>(
                _lists.Values.Where(x => x.FamilyId == familyId).Select(Clone).ToList());
        }
    }

    Task<DomainKinList?> IKinListRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(_lists.TryGetValue(id, out var list) ? Clone(list) : null);
        }
    }

    public Task<DomainKinList> AddAsync(DomainKinList list, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            _lists[list.Id] = Clone(list);
            return Task.FromResult(Clone(list));
        }
    }

    public Task<DomainKinList> UpdateAsync(DomainKinList list, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            _lists[list.Id] = Clone(list);
            return Task.FromResult(Clone(list));
        }
    }

    public Task<IReadOnlyList<DomainKinListItem>> GetAllByListIdAsync(Guid listId, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<DomainKinListItem>>(
                _items.Values.Where(x => x.ListId == listId)
                    .OrderBy(x => x.IsCompleted)
                    .ThenByDescending(x => x.ActivationOrder)
                    .Select(Clone)
                    .ToList());
        }
    }

    Task<DomainKinListItem?> IKinListItemRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(_items.TryGetValue(id, out var item) ? Clone(item) : null);
        }
    }

    public Task<DomainKinListItem> AddAsync(DomainKinListItem item, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            _items[item.Id] = Clone(item);
            return Task.FromResult(Clone(item));
        }
    }

    public Task<DomainKinListItem> UpdateAsync(DomainKinListItem item, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            _items[item.Id] = Clone(item);
            return Task.FromResult(Clone(item));
        }
    }

    public Task<long> GetNextActivationOrderAsync(Guid listId, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var max = _items.Values.Where(x => x.ListId == listId && !x.IsDeleted).Select(x => (long?)x.ActivationOrder).Max();
            return Task.FromResult(max is { } value ? value + 1 : 1);
        }
    }

    public Task<IdempotencyRecord?> GetActiveAsync(string key, Guid familyId, Guid userId, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var record = _records.LastOrDefault(x => x.Key == key && x.FamilyId == familyId && x.UserId == userId && x.ExpiresAt > utcNow);
            return Task.FromResult(record is null ? null : Clone(record));
        }
    }

    public Task DeleteExpiredAsync(string key, Guid familyId, Guid userId, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            _records.RemoveAll(x => x.Key == key && x.FamilyId == familyId && x.UserId == userId && x.ExpiresAt <= utcNow);
            return Task.CompletedTask;
        }
    }

    public Task<int> DeleteExpiredAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var removed = _records.RemoveAll(x => x.ExpiresAt <= utcNow);
            return Task.FromResult(removed);
        }
    }

    public Task<IdempotencyRecord> AddAsync(IdempotencyRecord record, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            _records.Add(Clone(record));
            return Task.FromResult(Clone(record));
        }
    }

    public int IdempotencyRecordCount
    {
        get
        {
            lock (_gate)
            {
                return _records.Count;
            }
        }
    }

    public void SeedIdempotencyRecord(IdempotencyRecord record)
    {
        lock (_gate)
        {
            _records.Add(Clone(record));
        }
    }

    private static DomainKinList Clone(DomainKinList list) => new()
    {
        Id = list.Id,
        FamilyId = list.FamilyId,
        Title = list.Title,
        Version = list.Version,
        IsDeleted = list.IsDeleted,
        CreatedAt = list.CreatedAt,
        UpdatedAt = list.UpdatedAt,
        LastModifiedAt = list.LastModifiedAt,
    };

    private static DomainKinListItem Clone(DomainKinListItem item) => new()
    {
        Id = item.Id,
        ListId = item.ListId,
        Text = item.Text,
        Version = item.Version,
        IsCompleted = item.IsCompleted,
        ActivationOrder = item.ActivationOrder,
        IsDeleted = item.IsDeleted,
        CreatedAt = item.CreatedAt,
        UpdatedAt = item.UpdatedAt,
    };

    private static IdempotencyRecord Clone(IdempotencyRecord record) => new()
    {
        Id = record.Id,
        Key = record.Key,
        FamilyId = record.FamilyId,
        UserId = record.UserId,
        RequestHash = record.RequestHash,
        ResponseJson = record.ResponseJson,
        ExpiresAt = record.ExpiresAt,
        CreatedAt = record.CreatedAt,
    };
}
