using System.Text.Json;

namespace Kin.KinHub.Core.Test;

public sealed class InfrastructureTemplateTests
{
    [Fact]
    public void MainTemplate_ContainsManualMigrationJob_WithKeyVaultBackedSecrets()
    {
        using var document = LoadJson("ops", "iac", "main.json");
        var migrationJob = FindResource(document.RootElement, "Microsoft.App/jobs", "[parameters('kinListMigrationJobName')]");

        var configuration = migrationJob.GetProperty("properties").GetProperty("configuration");
        Assert.Equal("Manual", configuration.GetProperty("triggerType").GetString());

        var secrets = configuration.GetProperty("secrets").EnumerateArray().ToArray();
        Assert.Contains(secrets, secret => HasSecret(secret, "db-connection-string"));
        Assert.Contains(secrets, secret => HasSecret(secret, "ghcr-password"));
        Assert.All(secrets, secret => Assert.True(secret.TryGetProperty("keyVaultUrl", out _), "Migration job secrets must use Key Vault references."));

        var container = migrationJob.GetProperty("properties").GetProperty("template").GetProperty("containers")[0];
        Assert.Equal("[parameters('kinListMigrationImage')]", container.GetProperty("image").GetString());
        Assert.Contains(
            container.GetProperty("env").EnumerateArray(),
            env => env.GetProperty("name").GetString() == "ConnectionStrings__KinHub"
                   && env.GetProperty("secretRef").GetString() == "db-connection-string");
    }

    [Fact]
    public void MainTemplate_WiresKinListToIdentity_WithAudienceAndKeyVaultSecrets()
    {
        using var document = LoadJson("ops", "iac", "main.json");
        var kinListApp = FindResource(document.RootElement, "Microsoft.App/containerApps", "[parameters('kinListContainerAppName')]");

        var secrets = kinListApp.GetProperty("properties").GetProperty("configuration").GetProperty("secrets").EnumerateArray().ToArray();
        Assert.Contains(secrets, secret => HasSecret(secret, "db-connection-string"));
        Assert.Contains(secrets, secret => HasSecret(secret, "jwt-secret"));
        Assert.Contains(secrets, secret => HasSecret(secret, "openai-key"));
        Assert.Contains(secrets, secret => HasSecret(secret, "speech-key"));
        Assert.All(secrets, secret => Assert.True(secret.TryGetProperty("keyVaultUrl", out _), "KinList container secrets must use Key Vault references."));

        var envExpression = kinListApp
            .GetProperty("properties")
            .GetProperty("template")
            .GetProperty("containers")[0]
            .GetProperty("env")
            .GetString();

        Assert.NotNull(envExpression);
        Assert.Contains("FamilyContextApi__BaseUrl", envExpression, StringComparison.Ordinal);
        Assert.Contains("parameters('identityContainerAppName')", envExpression, StringComparison.Ordinal);
        Assert.Contains("Jwt__Audience", envExpression, StringComparison.Ordinal);
        Assert.Contains("kinhub.api", envExpression, StringComparison.Ordinal);
    }

    [Fact]
    public void MainTemplate_ExportsKinListMigrationJobName_AndAvoidsLegacyCoreApiVariable()
    {
        using var document = LoadJson("ops", "iac", "main.json");
        var outputs = document.RootElement.GetProperty("outputs");

        Assert.Equal(
            "[parameters('kinListMigrationJobName')]",
            outputs.GetProperty("kinListMigrationJobName").GetProperty("value").GetString());

        var templateJson = document.RootElement.GetRawText();
        Assert.DoesNotContain("KINLIST_CORE_API_BASE_URL", templateJson, StringComparison.Ordinal);
    }

    [Fact]
    public void ManagedIdentitiesTemplate_DeclaresAllRequiredUserAssignedIdentities()
    {
        using var document = LoadJson("ops", "iac", "managed-identities.json");
        var resources = document.RootElement.GetProperty("resources").EnumerateArray().ToArray();

        Assert.Contains(resources, resource => HasResourceName(resource, "[format('{0}-identity', parameters('identityContainerAppName'))]"));
        Assert.Contains(resources, resource => HasResourceName(resource, "[format('{0}-identity', parameters('kinRecipeContainerAppName'))]"));
        Assert.Contains(resources, resource => HasResourceName(resource, "[format('{0}-identity', parameters('kinListContainerAppName'))]"));
        Assert.Contains(resources, resource => HasResourceName(resource, "[format('{0}-identity', parameters('kinListMigrationJobName'))]"));

        var outputs = document.RootElement.GetProperty("outputs");
        Assert.True(outputs.TryGetProperty("identityIdentityId", out _));
        Assert.True(outputs.TryGetProperty("kinRecipeIdentityId", out _));
        Assert.True(outputs.TryGetProperty("kinListIdentityId", out _));
        Assert.True(outputs.TryGetProperty("kinListMigrationIdentityId", out _));
    }

    private static bool HasSecret(JsonElement secret, string name) =>
        secret.GetProperty("name").GetString() == name;

    private static bool HasResourceName(JsonElement resource, string name) =>
        resource.GetProperty("name").GetString() == name;

    private static JsonElement FindResource(JsonElement root, string type, string name)
    {
        foreach (var resource in root.GetProperty("resources").EnumerateArray())
        {
            if (resource.GetProperty("type").GetString() == type
                && resource.GetProperty("name").GetString() == name)
            {
                return resource;
            }
        }

        throw new Xunit.Sdk.XunitException($"Resource '{type}' / '{name}' not found.");
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
