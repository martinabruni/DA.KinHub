using System.Text.Json;

namespace Kin.KinHub.Core.Test;

public sealed class InfrastructureTemplateTests
{
    [Fact]
    public void MainTemplate_ProvisionsFunctionApp_OnConsumptionPlan_WithKeyVaultBackedSettings()
    {
        using var document = LoadJson("ops", "iac", "main.json");
        var functionApp = FindResource(document.RootElement, "Microsoft.Web/sites", "[parameters('functionAppName')]");
        var plan = FindResource(document.RootElement, "Microsoft.Web/serverfarms", "[format('{0}-plan', parameters('functionAppName'))]");

        Assert.Equal("Y1", plan.GetProperty("sku").GetProperty("name").GetString());
        Assert.Equal("Dynamic", plan.GetProperty("sku").GetProperty("tier").GetString());
        Assert.Equal("functionapp", functionApp.GetProperty("kind").GetString());

        var appSettingsExpression = functionApp
            .GetProperty("properties")
            .GetProperty("siteConfig")
            .GetProperty("appSettings")
            .GetString();

        Assert.NotNull(appSettingsExpression);
        Assert.Contains("'AzureWebJobsStorage'", appSettingsExpression, StringComparison.Ordinal);
        Assert.Contains("'ConnectionStrings__KinHub'", appSettingsExpression, StringComparison.Ordinal);
        Assert.Contains("'Jwt__Secret'", appSettingsExpression, StringComparison.Ordinal);
        Assert.Contains("@Microsoft.KeyVault(SecretUri=", appSettingsExpression, StringComparison.Ordinal);
        Assert.Contains("parameters('storageConnectionStringSecretUri')", appSettingsExpression, StringComparison.Ordinal);
        Assert.Contains("parameters('sqlConnectionStringSecretUri')", appSettingsExpression, StringComparison.Ordinal);
    }

    [Fact]
    public void MainTemplate_WiresFunctionAppToIdentity_WithAudienceAndManagedIdentityAuth()
    {
        using var document = LoadJson("ops", "iac", "main.json");
        var functionApp = FindResource(document.RootElement, "Microsoft.Web/sites", "[parameters('functionAppName')]");

        var appSettingsExpression = functionApp
            .GetProperty("properties")
            .GetProperty("siteConfig")
            .GetProperty("appSettings")
            .GetString();

        Assert.NotNull(appSettingsExpression);
        Assert.Contains("FamilyContextApi__BaseUrl", appSettingsExpression, StringComparison.Ordinal);
        Assert.Contains("parameters('identityContainerAppName')", appSettingsExpression, StringComparison.Ordinal);
        Assert.Contains("Jwt__Audience", appSettingsExpression, StringComparison.Ordinal);
        Assert.Contains("kinhub.api", appSettingsExpression, StringComparison.Ordinal);
        Assert.Contains("OpenAi__UseManagedIdentity", appSettingsExpression, StringComparison.Ordinal);
        Assert.Contains("Speech__UseManagedIdentity", appSettingsExpression, StringComparison.Ordinal);
        Assert.DoesNotContain("openAiKeySecretUri", appSettingsExpression, StringComparison.Ordinal);
        Assert.DoesNotContain("speechKeySecretUri", appSettingsExpression, StringComparison.Ordinal);
    }

    [Fact]
    public void MainTemplate_ExportsNonIdentityApiUrl_AndAvoidsLegacyCoreApiVariable()
    {
        using var document = LoadJson("ops", "iac", "main.json");
        var computeDeployment = FindResource(document.RootElement, "Microsoft.Resources/deployments", "compute");
        var outputs = document.RootElement.GetProperty("outputs");

        Assert.Equal(
            "[reference('compute').outputs.nonIdentityApiUrl.value]",
            outputs.GetProperty("nonIdentityApiUrl").GetProperty("value").GetString());
        Assert.True(
            computeDeployment
                .GetProperty("properties")
                .GetProperty("template")
                .GetProperty("outputs")
                .TryGetProperty("nonIdentityApiUrl", out _));

        var templateJson = document.RootElement.GetRawText();
        Assert.DoesNotContain("KINLIST_CORE_API_BASE_URL", templateJson, StringComparison.Ordinal);
    }

    [Fact]
    public void ManagedIdentitiesTemplate_DeclaresAllRequiredUserAssignedIdentities()
    {
        using var document = LoadJson("ops", "iac", "managed-identities.json");
        var resources = document.RootElement.GetProperty("resources").EnumerateArray().ToArray();

        Assert.Contains(resources, resource => HasResourceName(resource, "[format('{0}-identity', parameters('identityContainerAppName'))]"));
        Assert.Contains(resources, resource => HasResourceName(resource, "[format('{0}-identity', parameters('functionAppName'))]"));
        Assert.Equal(2, resources.Length);

        var outputs = document.RootElement.GetProperty("outputs");
        Assert.True(outputs.TryGetProperty("identityIdentityId", out _));
        Assert.True(outputs.TryGetProperty("functionAppIdentityId", out _));
    }

    private static bool HasSecret(JsonElement secret, string name) =>
        secret.GetProperty("name").GetString() == name;

    private static bool HasResourceName(JsonElement resource, string name) =>
        resource.GetProperty("name").GetString() == name;

    private static JsonElement FindResource(JsonElement root, string type, string name)
    {
        foreach (var resource in EnumerateResources(root))
        {
            if (resource.GetProperty("type").GetString() == type
                && resource.GetProperty("name").GetString() == name)
            {
                return resource;
            }
        }

        throw new Xunit.Sdk.XunitException($"Resource '{type}' / '{name}' not found.");
    }

    private static IEnumerable<JsonElement> EnumerateResources(JsonElement node)
    {
        if (!node.TryGetProperty("resources", out var resources))
        {
            yield break;
        }

        foreach (var resource in EnumerateResourceCollection(resources))
        {
            yield return resource;

            foreach (var nested in EnumerateResources(resource))
            {
                yield return nested;
            }

            if (resource.TryGetProperty("properties", out var properties)
                && properties.TryGetProperty("template", out var template))
            {
                foreach (var nested in EnumerateResources(template))
                {
                    yield return nested;
                }
            }
        }
    }

    private static IEnumerable<JsonElement> EnumerateResourceCollection(JsonElement resources)
    {
        if (resources.ValueKind == JsonValueKind.Array)
        {
            foreach (var resource in resources.EnumerateArray())
            {
                yield return resource;
            }

            yield break;
        }

        if (resources.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in resources.EnumerateObject())
            {
                yield return property.Value;
            }
        }
    }

    private static JsonDocument LoadJson(params string[] relativePathSegments)
    {
        var repoRoot = FindRepositoryRoot();
        var segments = new[] { repoRoot.FullName }.Concat(relativePathSegments).ToArray();
        var path = Path.Combine(segments);
        return JsonDocument.Parse(File.ReadAllText(path));
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Kin.KinHub.Core.slnx")))
            {
                return current;
            }

            current = current.Parent;
        }

        throw new Xunit.Sdk.XunitException("Repository root not found from test base directory.");
    }
}
