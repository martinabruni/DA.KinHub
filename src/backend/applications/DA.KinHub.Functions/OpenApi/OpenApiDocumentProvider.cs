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
                [$"/{ApiRoutes.KinHub.FamilyDetails}"] = new
                {
                    get = FamilyOperation(
                        "Read the active family name for the authorized family",
                        new Dictionary<string, object>
                        {
                            ["200"] = new
                            {
                                description = "Family details",
                                content = new Dictionary<string, object>
                                {
                                    ["application/json"] = new
                                    {
                                        schema = new { @ref = "#/components/schemas/FamilyDetails" }
                                    }
                                }
                            },
                            ["400"] = problemResponse,
                            ["401"] = problemResponse,
                            ["403"] = problemResponse,
                            ["409"] = problemResponse,
                            ["500"] = problemResponse,
                            ["503"] = problemResponse
                        })
                },
                [$"/{ApiRoutes.KinHub.FamilyMembers}"] = new
                {
                    get = FamilyOperation(
                        "Read a keyset page of active family members for the authorized family",
                        new Dictionary<string, object>
                        {
                            ["200"] = new
                            {
                                description = "Family members page",
                                content = new Dictionary<string, object>
                                {
                                    ["application/json"] = new
                                    {
                                        schema = new { @ref = "#/components/schemas/FamilyMembersPage" }
                                    }
                                }
                            },
                            ["400"] = problemResponse,
                            ["401"] = problemResponse,
                            ["403"] = problemResponse,
                            ["409"] = problemResponse,
                            ["500"] = problemResponse,
                            ["503"] = problemResponse
                        },
                        new object[]
                        {
                            new
                            {
                                name = "pageSize",
                                @in = "query",
                                required = true,
                                schema = new { type = "integer", format = "int32", minimum = 1 }
                            },
                            new
                            {
                                name = "cursor",
                                @in = "query",
                                required = false,
                                schema = new { type = "string" }
                            }
                        })
                },
                 [$"/{ApiRoutes.KinHub.FamilyInvitations}"] = new
                 {
                     get = FamilyOperation(
                        "Read a keyset page of active family invitations for the authorized family",
                        new Dictionary<string, object>
                        {
                            ["200"] = new
                            {
                                description = "Family invitations page",
                                content = new Dictionary<string, object>
                                {
                                    ["application/json"] = new
                                    {
                                        schema = new { @ref = "#/components/schemas/FamilyInvitationsPage" }
                                    }
                                }
                            },
                            ["400"] = problemResponse,
                            ["401"] = problemResponse,
                            ["403"] = problemResponse,
                            ["409"] = problemResponse,
                            ["500"] = problemResponse,
                            ["503"] = problemResponse
                        },
                        new object[]
                        {
                            new
                            {
                                name = "pageSize",
                                @in = "query",
                                required = true,
                                schema = new { type = "integer", format = "int32", minimum = 1 }
                            },
                            new
                            {
                                name = "cursor",
                                @in = "query",
                                required = false,
                                schema = new { type = "string" }
                            }
                         }),
                     post = FamilyOperation(
                         "Create an invitation for the authorized family",
                         new Dictionary<string, object>
                         {
                             ["201"] = new { description = "Invitation created" },
                             ["400"] = problemResponse,
                             ["401"] = problemResponse,
                             ["403"] = problemResponse,
                             ["409"] = problemResponse,
                             ["500"] = problemResponse,
                             ["503"] = problemResponse
                         })
                  },
                 [$"/{ApiRoutes.KinHub.FamilyInvitationById}"] = new
                 {
                     delete = FamilyOperation(
                         "Revoke an active family invitation",
                         new Dictionary<string, object>
                         {
                             ["204"] = new { description = "Invitation revoked" },
                             ["400"] = problemResponse,
                             ["401"] = problemResponse,
                             ["403"] = problemResponse,
                             ["404"] = problemResponse,
                             ["409"] = problemResponse,
                             ["500"] = problemResponse,
                             ["503"] = problemResponse
                         },
                         new object[]
                         {
                             new
                             {
                                 name = "invitationId",
                                 @in = "path",
                                 required = true,
                                 schema = new { type = "string", format = "uuid" }
                             }
                          })
                  },
                 [$"/{ApiRoutes.KinHub.FamilyJoin}"] = new
                 {
                     post = ProtectedOperation(
                         "Join a family using an invitation code",
                         new Dictionary<string, object>
                         {
                             ["200"] = new { description = "Family joined" },
                             ["400"] = problemResponse,
                             ["401"] = problemResponse,
                             ["409"] = problemResponse,
                             ["429"] = problemResponse,
                             ["500"] = problemResponse,
                             ["503"] = problemResponse
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
                },
                [$"/{ApiRoutes.KinHub.Services}"] = new
                {
                    get = FamilyOperation(
                        "List the KinServices available for the authorized family",
                        new Dictionary<string, object>
                        {
                            ["200"] = new
                            {
                                description = "Family service catalog",
                                content = new Dictionary<string, object>
                                {
                                    ["application/json"] = new
                                    {
                                        schema = new { @ref = "#/components/schemas/KinHubServiceCatalogResult" }
                                    }
                                }
                            },
                            ["400"] = problemResponse,
                            ["401"] = problemResponse,
                            ["403"] = problemResponse,
                            ["500"] = problemResponse,
                            ["503"] = problemResponse
                        },
                        new object[]
                        {
                            new
                            {
                                name = "language",
                                @in = "query",
                                required = false,
                                schema = new { type = "string", @enum = new[] { "it", "en" } }
                            }
                        })
                },
                [$"/{ApiRoutes.KinHub.ServiceAccess.Replace("{serviceKey}", "{serviceKey}", StringComparison.Ordinal)}"] = new
                {
                    get = FamilyOperation(
                        "Check whether the authorized family can access a KinService",
                        new Dictionary<string, object>
                        {
                            ["204"] = new { description = "Access granted" },
                            ["400"] = problemResponse,
                            ["401"] = problemResponse,
                            ["403"] = problemResponse,
                            ["500"] = problemResponse,
                            ["503"] = problemResponse
                        },
                        new object[]
                        {
                            new
                            {
                                name = "serviceKey",
                                @in = "path",
                                required = true,
                                schema = new { type = "string" }
                            }
                        })
                },
                [$"/{ApiRoutes.KinList.Items}"] = new
                {
                    get = FamilyOperation(
                        "Read a keyset page of active KinList items visible to the authorized family",
                        new Dictionary<string, object>
                        {
                            ["200"] = new
                            {
                                description = "Active KinList items page",
                                content = new Dictionary<string, object>
                                {
                                    ["application/json"] = new
                                    {
                                        schema = new { @ref = "#/components/schemas/ActiveItemsPage" }
                                    }
                                }
                            },
                            ["400"] = problemResponse,
                            ["401"] = problemResponse,
                            ["403"] = problemResponse,
                            ["500"] = problemResponse,
                            ["503"] = problemResponse
                        },
                        new object[]
                        {
                            new
                            {
                                name = "pageSize",
                                @in = "query",
                                required = true,
                                schema = new { type = "integer", format = "int32", minimum = 1 }
                            },
                            new
                            {
                                name = "cursor",
                                @in = "query",
                                required = false,
                                schema = new { type = "string" }
                            }
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
                    ["FamilyDetails"] = new
                    {
                        type = "object",
                        required = new[] { "name" },
                        properties = new Dictionary<string, object>
                        {
                            ["name"] = new { type = "string" }
                        }
                    },
                    ["FamilyMember"] = new
                    {
                        type = "object",
                        required = new[] { "displayName", "initials", "isCurrentUser" },
                        properties = new Dictionary<string, object>
                        {
                            ["displayName"] = new { type = "string", nullable = true },
                            ["initials"] = new { type = "string", nullable = true },
                            ["isCurrentUser"] = new { type = "boolean" }
                        }
                    },
                    ["FamilyMembersPage"] = new
                    {
                        type = "object",
                        required = new[] { "items", "effectivePageSize", "maxPageSize", "previousCursor", "nextCursor" },
                        properties = new Dictionary<string, object>
                        {
                            ["items"] = new
                            {
                                type = "array",
                                items = new { @ref = "#/components/schemas/FamilyMember" }
                            },
                            ["effectivePageSize"] = new { type = "integer", format = "int32" },
                            ["maxPageSize"] = new { type = "integer", format = "int32" },
                            ["previousCursor"] = new { type = "string", nullable = true },
                            ["nextCursor"] = new { type = "string", nullable = true }
                        }
                    },
                    ["FamilyInvitationCreator"] = new
                    {
                        type = "object",
                        required = new[] { "displayName", "initials" },
                        properties = new Dictionary<string, object>
                        {
                            ["displayName"] = new { type = "string", nullable = true },
                            ["initials"] = new { type = "string", nullable = true }
                        }
                    },
                    ["FamilyInvitation"] = new
                    {
                        type = "object",
                        required = new[] { "id", "creator", "createdAt", "expiresAt", "status" },
                        properties = new Dictionary<string, object>
                        {
                            ["id"] = new { type = "string", format = "uuid" },
                            ["creator"] = new { @ref = "#/components/schemas/FamilyInvitationCreator" },
                            ["createdAt"] = new { type = "string", format = "date-time" },
                            ["expiresAt"] = new { type = "string", format = "date-time" },
                            ["status"] = new { type = "string", @enum = new[] { "active" } }
                        }
                    },
                    ["FamilyInvitationsPage"] = new
                    {
                        type = "object",
                        required = new[] { "items", "effectivePageSize", "maxPageSize", "previousCursor", "nextCursor" },
                        properties = new Dictionary<string, object>
                        {
                            ["items"] = new
                            {
                                type = "array",
                                items = new { @ref = "#/components/schemas/FamilyInvitation" }
                            },
                            ["effectivePageSize"] = new { type = "integer", format = "int32" },
                            ["maxPageSize"] = new { type = "integer", format = "int32" },
                            ["previousCursor"] = new { type = "string", nullable = true },
                            ["nextCursor"] = new { type = "string", nullable = true }
                        }
                    },
                    ["KinHubServiceCatalogItem"] = new
                    {
                        type = "object",
                        required = new[] { "key", "route", "name", "description" },
                        properties = new Dictionary<string, object>
                        {
                            ["key"] = new { type = "string" },
                            ["route"] = new { type = "string" },
                            ["name"] = new { type = "string" },
                            ["description"] = new { type = "string" }
                        }
                    },
                    ["KinHubServiceCatalogResult"] = new
                    {
                        type = "object",
                        required = new[] { "services" },
                        properties = new Dictionary<string, object>
                        {
                            ["services"] = new
                            {
                                type = "array",
                                items = new { @ref = "#/components/schemas/KinHubServiceCatalogItem" }
                            }
                        }
                    },
                    ["ActiveItemsPageAuthor"] = new
                    {
                        type = "object",
                        required = new[] { "displayName" },
                        properties = new Dictionary<string, object>
                        {
                            ["displayName"] = new { type = "string", nullable = true }
                        }
                    },
                    ["ActiveItemsPageCategory"] = new
                    {
                        type = "object",
                        required = new[] { "id", "name" },
                        properties = new Dictionary<string, object>
                        {
                            ["id"] = new { type = "string", format = "uuid" },
                            ["name"] = new { type = "string" }
                        }
                    },
                    ["ActiveItemsPageItem"] = new
                    {
                        type = "object",
                        required = new[] { "id", "name", "categories", "remainingCategoryCount", "author", "version" },
                        properties = new Dictionary<string, object>
                        {
                            ["id"] = new { type = "string", format = "uuid" },
                            ["name"] = new { type = "string" },
                            ["categories"] = new
                            {
                                type = "array",
                                items = new { @ref = "#/components/schemas/ActiveItemsPageCategory" }
                            },
                            ["remainingCategoryCount"] = new { type = "integer", format = "int32" },
                            ["author"] = new { @ref = "#/components/schemas/ActiveItemsPageAuthor" },
                            ["version"] = new { type = "string" }
                        }
                    },
                    ["ActiveItemsPage"] = new
                    {
                        type = "object",
                        required = new[] { "items", "effectivePageSize", "maxPageSize", "previousCursor", "nextCursor" },
                        properties = new Dictionary<string, object>
                        {
                            ["items"] = new
                            {
                                type = "array",
                                items = new { @ref = "#/components/schemas/ActiveItemsPageItem" }
                            },
                            ["effectivePageSize"] = new { type = "integer", format = "int32" },
                            ["maxPageSize"] = new { type = "integer", format = "int32" },
                            ["previousCursor"] = new { type = "string", nullable = true },
                            ["nextCursor"] = new { type = "string", nullable = true }
                        }
                    },
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

    private static object FamilyOperation(string summary, Dictionary<string, object> responses, object[]? additionalParameters = null)
    {
        var parameters = new List<object>
        {
            new
            {
                name = SecurityConstants.FamilyIdQueryParameter,
                @in = "query",
                required = true,
                schema = new { type = "string", format = "uuid" }
            }
        };

        if (additionalParameters is not null)
        {
            parameters.AddRange(additionalParameters);
        }

        return new
        {
            summary,
            responses,
            security = new object[] { new Dictionary<string, object> { [SecurityConstants.BearerScheme] = Array.Empty<string>() } },
            parameters,
            x_cacheControl = ApiResults.NoStorePrivateCacheControl
        };
    }
}
