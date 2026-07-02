using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Kin.KinHub.KinList.Api.Common;

public sealed class RemoteFamilyOwnershipService : IFamilyOwnershipService
{
    private const string MissingAuthorizationMessage = "Missing or invalid Authorization header.";
    private const string FamilyContextUnavailableMessage = "Family context could not be resolved because Identity is unavailable.";

    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<RemoteFamilyOwnershipService> _logger;

    public RemoteFamilyOwnershipService(
        HttpClient httpClient,
        IHttpContextAccessor httpContextAccessor,
        ILogger<RemoteFamilyOwnershipService> logger)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<FamilyAccessResult> GetCurrentFamilyAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var authorizationHeader = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authorizationHeader)
            || !AuthenticationHeaderValue.TryParse(authorizationHeader, out var parsedAuthorizationHeader))
        {
            return FamilyAccessResult.Unauthorized(MissingAuthorizationMessage);
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, "api/access/family-context");
        request.Headers.Authorization = parsedAuthorizationHeader;

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var payload = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                if (!payload.RootElement.TryGetProperty("familyId", out var familyIdElement)
                    || !familyIdElement.TryGetGuid(out var familyId))
                {
                    _logger.LogError("Identity family-context response did not contain a valid familyId.");
                    return FamilyAccessResult.ServiceUnavailable(FamilyContextUnavailableMessage);
                }

                return FamilyAccessResult.Success(new Family
                {
                    Id = familyId,
                    Name = string.Empty,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                });
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                return FamilyAccessResult.NotFound("Family not found for this user.");
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return FamilyAccessResult.Unauthorized(MissingAuthorizationMessage);
            }

            _logger.LogWarning("Identity family-context request failed with status code {StatusCode}.", (int)response.StatusCode);
            return FamilyAccessResult.ServiceUnavailable(FamilyContextUnavailableMessage);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Identity family-context request timed out.");
            return FamilyAccessResult.ServiceUnavailable(FamilyContextUnavailableMessage);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Identity family-context request failed.");
            return FamilyAccessResult.ServiceUnavailable(FamilyContextUnavailableMessage);
        }
    }

    public async Task<FamilyAccessResult> EnsureOwnershipAsync(Guid familyId, Guid userId, CancellationToken cancellationToken = default)
    {
        var currentFamily = await GetCurrentFamilyAsync(userId, cancellationToken);
        if (!currentFamily.IsSuccess)
        {
            return currentFamily;
        }

        if (currentFamily.Family!.Id != familyId)
        {
            return FamilyAccessResult.Unauthorized("You do not own this family.");
        }

        return currentFamily;
    }
}
