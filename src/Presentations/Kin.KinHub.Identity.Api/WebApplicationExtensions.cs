using Kin.KinHub.Identity.Api.Common.Configuration;
using Kin.KinHub.Identity.Api.Common.Middlewares;

namespace Kin.KinHub.Identity.Api;

public static class WebApplicationExtensions
{
    public static WebApplication UseKinHubIdentityApi(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseForwardedHeaders();
        app.UseHttpsRedirection();
        app.UseCors(CorsOptions.PolicyName);
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseMiddleware<JwtAuthenticationMiddleware>();
        app.UseAuthorization();
        app.MapControllers();
        app.MapHealthChecks("/health", new()
        {
            Predicate = _ => false,
        }).AllowAnonymous();
        app.MapHealthChecks("/health/ready", new()
        {
            Predicate = registration => registration.Tags.Contains("ready"),
        }).AllowAnonymous();

        return app;
    }
}
