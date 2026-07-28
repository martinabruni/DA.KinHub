using DA.KinHub.Functions.Configuration;
using DA.KinHub.Functions.Http;
using DA.KinHub.Functions.Security;
using Microsoft.Extensions.Options;

namespace DA.KinHub.Functions.OpenApi;

public sealed class OpenApiDocumentProvider(BuildInfoProvider buildInfoProvider, IOptions<EntraOptions> entraOptions)
{
    public object GetDocument()
    {
        var entra = entraOptions.Value;
        var authority = $"{entra.Instance.TrimEnd('/')}/{entra.TenantId}/oauth2/v2.0";
        var apiScope = $"api://{entra.Audience}/{entra.Scope}";

        var problemResponse = new Dictionary<string, object>
        {
            ["description"] = "Problem Details",
            ["content"] = new Dictionary<string, object>
            {
                [ApiResults.ProblemMediaType] = new
                {
                    schema = new { @ref = "#/components/schemas/ProblemDetails" }
                }
            }
        };

        return new
        {
            openapi = "3.0.3",
            info = new { title = "KinHub API", version = buildInfoProvider.Get().ApiVersion },
            paths = new Dictionary<string, object>
            {
                [$"/{ApiRoutes.Health.Live}"] = new { get = PublicOperation("Liveness", new Dictionary<string, object> { ["200"] = new { description = "Healthy" } }) },
                [$"/{ApiRoutes.Health.Ready}"] = new { get = PublicOperation("Readiness", new Dictionary<string, object> { ["200"] = new { description = "Ready" }, ["503"] = new { description = "Not ready" } }) },
                [$"/{ApiRoutes.Metadata.Version}"] = new { get = PublicOperation("Build metadata", new Dictionary<string, object> { ["200"] = new { description = "Version" } }) },
                [$"/{ApiRoutes.Metadata.Status}"] = new { get = PublicOperation("Application status", new Dictionary<string, object> { ["200"] = new { description = "Status" } }) },
                [$"/{ApiRoutes.Metadata.OpenApi}"] = new { get = PublicOperation("OpenAPI document", new Dictionary<string, object> { ["200"] = new { description = "Document" } }) },
                [$"/{ApiRoutes.KinHub.Bootstrap}"] = new
                {
                    get = ProtectedOperation(
                        "Resolve the KinHub post-login state",
                        new Dictionary<string, object>
                        {
                            ["200"] = new { description = "Bootstrap resolved" },
                            ["401"] = problemResponse,
                            ["403"] = problemResponse,
                            ["500"] = problemResponse,
                            ["503"] = problemResponse
                        })
                },
                [$"/{ApiRoutes.KinHub.Families}"] = new
                {
                    post = ProtectedOperation(
                        "Create the first family for the signed-in user",
                        new Dictionary<string, object>
                        {
                            ["201"] = new { description = "Family created" },
                            ["200"] = new { description = "Existing family returned" },
                            ["400"] = problemResponse,
                            ["401"] = problemResponse,
                            ["403"] = problemResponse,
                            ["500"] = problemResponse,
                            ["503"] = problemResponse
                        },
                        new
                        {
                            required = true,
                            content = new Dictionary<string, object>
                            {
                                ["application/json"] = new
                                {
                                    schema = new
                                    {
                                        type = "object",
                                        required = new[] { "name" },
                                        properties = new Dictionary<string, object>
                                        {
                                            ["name"] = new { type = "string", maxLength = 100 }
                                        }
                                    }
                                }
                            }
                        })
                },
                [$"/{ApiRoutes.KinHub.FamilyContext}"] = new
                {
                    get = FamilyOperation(
                        "Validate the Family policy for a familyId",
                        new Dictionary<string, object>
                        {
                            ["204"] = new { description = "Access granted" },
                            ["400"] = problemResponse,
                            ["401"] = problemResponse,
                            ["403"] = problemResponse,
                            ["500"] = problemResponse,
                            ["503"] = problemResponse
                        })
                }
            },
            components = new
            {
                securitySchemes = new Dictionary<string, object>
                {
                    [SecurityConstants.BearerScheme] = new { type = "http", scheme = "bearer", bearerFormat = "JWT" },
                    ["entraOAuth2"] = new
                    {
                        type = "oauth2",
                        flows = new
                        {
                            authorizationCode = new
                            {
                                authorizationUrl = $"{authority}/authorize",
                                tokenUrl = $"{authority}/token",
                                scopes = new Dictionary<string, string> { [apiScope] = "Access KinHub as the signed-in user" }
                            }
                        }
                    }
                },
                schemas = new Dictionary<string, object>
                {
                    ["ProblemDetails"] = new
                    {
                        type = "object",
                        properties = new Dictionary<string, object>
                        {
                            ["status"] = new { type = "integer", format = "int32" },
                            ["title"] = new { type = "string" },
                            ["detail"] = new { type = "string" },
                            ["instance"] = new { type = "string" },
                            [ApiProblemDetailsExtensions.Code] = new { type = "string" },
                            [ApiProblemDetailsExtensions.TraceId] = new { type = "string" },
                            [ApiProblemDetailsExtensions.CorrelationId] = new { type = "string" }
                        }
                    }
                }
            }
        };
    }

    private static object PublicOperation(string summary, Dictionary<string, object> responses) => new
    {
        summary,
        responses,
        x_cacheControl = ApiResults.NoStoreCacheControl
    };

    private static object ProtectedOperation(string summary, Dictionary<string, object> responses, object? requestBody = null)
    {
        var operation = new Dictionary<string, object>
        {
            ["summary"] = summary,
            ["responses"] = responses,
            ["security"] = new object[] { new Dictionary<string, object> { [SecurityConstants.BearerScheme] = Array.Empty<string>() } },
            ["x_cacheControl"] = ApiResults.NoStorePrivateCacheControl
        };

        if (requestBody is not null)
        {
            operation["requestBody"] = requestBody;
        }

        return operation;
    }

    private static object FamilyOperation(string summary, Dictionary<string, object> responses) => new
    {
        summary,
        responses,
        security = new object[] { new Dictionary<string, object> { [SecurityConstants.BearerScheme] = Array.Empty<string>() } },
        parameters = new object[]
        {
            new
            {
                name = SecurityConstants.FamilyIdQueryParameter,
                @in = "query",
                required = true,
                schema = new { type = "string", format = "uuid" }
            }
        },
        x_cacheControl = ApiResults.NoStorePrivateCacheControl
    };
}
