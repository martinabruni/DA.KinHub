using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using DA.KinHub.Functions.Configuration;
using DA.KinHub.Functions.Functions;
using DA.KinHub.Functions.OpenApi;
using DA.KinHub.Functions.Security;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Http;
using Microsoft.Extensions.Options;

namespace DA.KinHub.IntegrationTests;

public sealed class HttpFunctionContractTests
{
    private static readonly Assembly FunctionsAssembly = typeof(MetadataFunctions).Assembly;

    [Fact]
    public void EveryHttpFunctionHasDeterministicAccessMetadata()
    {
        var metadataProvider = new FunctionAccessMetadataProvider();

        foreach (var function in HttpFunctions())
        {
            var descriptor = metadataProvider.Get(Definition(function.EntryPoint));
            Assert.True(descriptor.IsHttp);

            var hasAllowAnonymous = function.Method.IsDefined(typeof(AllowAnonymousAttribute), inherit: true);
            var hasFamilyAccess = function.Method.IsDefined(typeof(RequiresFamilyAccessAttribute), inherit: true);

            Assert.Equal(hasAllowAnonymous, descriptor.AllowAnonymous);
            Assert.Equal(hasFamilyAccess, descriptor.RequiresFamilyAccess);
            Assert.False(hasAllowAnonymous && hasFamilyAccess, $"Function '{function.EntryPoint}' combines incompatible access markers.");
        }
    }

    [Fact]
    public void OpenApiDocumentsEveryHttpRouteWithExpectedSecurity()
    {
        var provider = new OpenApiDocumentProvider(
            new BuildInfoProvider(Options.Create(new RuntimeOptions { AppName = "KinHub", ApiVersion = "1.0", Environment = "Test" })),
            Options.Create(new EntraOptions
            {
                Enabled = true,
                Instance = "https://login.microsoftonline.com",
                TenantId = "contoso.onmicrosoft.com",
                Audience = "api://kinhub-test",
                Scope = "access_as_user"
            }));

        var document = JsonSerializer.SerializeToDocument(provider.GetDocument());
        var paths = document.RootElement.GetProperty("paths");

        foreach (var function in HttpFunctions())
        {
            var route = $"/{function.Route}";
            Assert.True(paths.TryGetProperty(route, out var pathItem), $"Route '{route}' is missing from OpenAPI.");
            Assert.True(pathItem.TryGetProperty("get", out var operation), $"Route '{route}' is missing the GET operation.");

            var hasSecurity = operation.TryGetProperty("security", out _);
            if (function.AllowAnonymous)
            {
                Assert.False(hasSecurity, $"Route '{route}' should be public in OpenAPI.");
            }
            else
            {
                Assert.True(hasSecurity, $"Route '{route}' should require bearer security in OpenAPI.");
            }

            if (function.RequiresFamilyAccess)
            {
                Assert.True(operation.TryGetProperty("parameters", out var parameters), $"Route '{route}' should declare the familyId query parameter.");
                Assert.Contains(parameters.EnumerateArray(), parameter =>
                    parameter.GetProperty("name").GetString() == SecurityConstants.FamilyIdQueryParameter
                    && parameter.GetProperty("in").GetString() == "query"
                    && parameter.GetProperty("required").GetBoolean());
            }
            else
            {
                if (operation.TryGetProperty("parameters", out var parameters))
                {
                    Assert.DoesNotContain(parameters.EnumerateArray(), parameter => parameter.GetProperty("name").GetString() == SecurityConstants.FamilyIdQueryParameter);
                }
            }
        }

        var documentedRoutes = paths.EnumerateObject().Select(path => path.Name).OrderBy(name => name).ToArray();
        var functionRoutes = HttpFunctions().Select(function => $"/{function.Route}").OrderBy(name => name).ToArray();
        Assert.Equal(functionRoutes, documentedRoutes);
    }

    private static IReadOnlyList<HttpFunctionMetadata> HttpFunctions()
    {
        return FunctionsAssembly
            .GetTypes()
            .Where(type => type.Namespace == typeof(MetadataFunctions).Namespace)
            .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            .Select(method => new
            {
                Method = method,
                FunctionAttribute = method.GetCustomAttribute<FunctionAttribute>(),
                Trigger = method.GetParameters()
                    .Select(parameter => parameter.GetCustomAttribute<HttpTriggerAttribute>())
                    .FirstOrDefault(attribute => attribute is not null)
            })
            .Where(candidate => candidate.FunctionAttribute is not null && candidate.Trigger is not null)
            .Select(candidate => new HttpFunctionMetadata(
                candidate.Method,
                $"{candidate.Method.DeclaringType!.FullName}.{candidate.Method.Name}",
                candidate.Trigger!.Route ?? string.Empty,
                candidate.Method.IsDefined(typeof(AllowAnonymousAttribute), inherit: true),
                candidate.Method.IsDefined(typeof(RequiresFamilyAccessAttribute), inherit: true)))
            .OrderBy(candidate => candidate.Route)
            .ToArray();
    }

    private static FunctionDefinition Definition(string entryPoint) => new StubFunctionDefinition(entryPoint);

    private sealed record HttpFunctionMetadata(MethodInfo Method, string EntryPoint, string Route, bool AllowAnonymous, bool RequiresFamilyAccess);

    private sealed class StubFunctionDefinition(string entryPoint) : FunctionDefinition
    {
        public override ImmutableArray<FunctionParameter> Parameters => ImmutableArray<FunctionParameter>.Empty;
        public override string PathToAssembly => FunctionsAssembly.Location;
        public override string EntryPoint => entryPoint;
        public override string Id => entryPoint;
        public override string Name => entryPoint;
        public override IImmutableDictionary<string, BindingMetadata> InputBindings => ImmutableDictionary<string, BindingMetadata>.Empty;
        public override IImmutableDictionary<string, BindingMetadata> OutputBindings => ImmutableDictionary<string, BindingMetadata>.Empty;
    }
}
