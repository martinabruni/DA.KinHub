using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Kin.KinHub.App.Functions.Common.Authorization;

namespace Kin.KinHub.App.Functions.Common;

public sealed class RemoteFamilyContextResolver : IFamilyContextResolver
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<RemoteFamilyContextResolver> _logger;

    public RemoteFamilyContextResolver(
        HttpClient httpClient,
        IHttpContextAccessor httpContextAccessor,
        ILogger<RemoteFamilyContextResolver> logger)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<FamilyContextResolution> ResolveAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var authorization = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authorization)
            || !AuthenticationHeaderValue.TryParse(authorization, out var parsed))
        {
            return FamilyContextResolution.Forbidden();
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, "api/access/family-context");
        request.Headers.Authorization = parsed;
        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode is HttpStatusCode.Unauthorized)
            {
                return FamilyContextResolution.Forbidden();
            }

            if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound)
            {
                return FamilyContextResolution.NoFamily();
            }

            if (response.StatusCode is not HttpStatusCode.OK)
            {
                return FamilyContextResolution.Unavailable();
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var payload = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            if (payload.RootElement.TryGetProperty("familyId", out var familyIdElement)
                && familyIdElement.TryGetGuid(out var familyId))
            {
                return FamilyContextResolution.Success(familyId);
            }

            _logger.LogError("Identity family-context response did not contain a valid familyId.");
            return FamilyContextResolution.Unavailable();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return FamilyContextResolution.Unavailable();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Identity family-context request failed.");
            return FamilyContextResolution.Unavailable();
        }
    }
}
