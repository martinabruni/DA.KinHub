using DA.KinHub.Functions.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DA.KinHub.Functions.Security;

public static class SecurityServiceCollectionExtensions
{
    public static IServiceCollection AddKinHubSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<EntraOptions>().BindConfiguration(EntraOptions.SectionName).ValidateOnStart();
        services.AddSingleton<IValidateOptions<EntraOptions>, EntraOptionsValidator>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<EntraOptions>>((options, entraOptions) =>
            {
                var entra = entraOptions.Value;
                options.Authority = $"{entra.Instance.TrimEnd('/')}/{entra.TenantId}/v2.0";
                options.Audience = entra.Audience;
                options.RequireHttpsMetadata = true;
                options.MapInboundClaims = false;
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(SecurityConstants.ApiAccessPolicy, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new ApiScopeRequirement()))
            .AddPolicy(SecurityConstants.FamilyPolicy, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new FamilyAuthorizationRequirement()));

        services.AddSingleton<ExternalIdentityClaimsResolver>();
        services.AddSingleton<FunctionAccessMetadataProvider>();
        services.AddScoped<RequestAuthenticationService>();
        services.AddScoped<IAuthorizationHandler, ApiScopeAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, FamilyAuthorizationHandler>();
        return services;
    }
}
