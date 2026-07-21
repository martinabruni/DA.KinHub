using Microsoft.AspNetCore.Http;

namespace DA.KinHub.Functions.Http;

public static class ApiResults
{
    public const string CorrelationIdHeaderName = "X-Correlation-ID";
    public const string ProblemMediaType = "application/problem+json";
    public const string NoStorePrivateCacheControl = "no-store, private";
    public const string NoStoreCacheControl = "no-store";
    private const int MaxCorrelationIdLength = 128;

    public static string EnsureCorrelationId(HttpContext httpContext)
    {
        var correlationId = TryGetCorrelationId(httpContext.Request.Headers[CorrelationIdHeaderName], out var requestedCorrelationId)
            ? requestedCorrelationId
            : Guid.NewGuid().ToString("N");

        httpContext.TraceIdentifier = correlationId;
        httpContext.Response.Headers[CorrelationIdHeaderName] = correlationId;
        return correlationId;
    }

    public static string GetCorrelationId(HttpContext httpContext)
    {
        return httpContext.Response.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationId) && !string.IsNullOrWhiteSpace(correlationId)
            ? correlationId.ToString()
            : httpContext.TraceIdentifier;
    }

    public static void ApplyNoStorePrivate(HttpResponse response) => response.Headers.CacheControl = NoStorePrivateCacheControl;

    public static void ApplyNoStore(HttpResponse response) => response.Headers.CacheControl = NoStoreCacheControl;

    private static bool TryGetCorrelationId(string? value, out string correlationId)
    {
        correlationId = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > MaxCorrelationIdLength || trimmed.Contains(','))
        {
            return false;
        }

        correlationId = trimmed;
        return true;
    }
}
