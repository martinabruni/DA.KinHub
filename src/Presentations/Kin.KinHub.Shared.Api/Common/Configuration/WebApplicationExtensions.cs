namespace Kin.KinHub.Shared.Api.Common.Configuration;

public static class WebApplicationExtensions
{
    public static WebApplication UseKinHubSharedApi(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();
        app.UseCors(CorsOptions.PolicyName);
        app.UseAuthentication();
        app.UseMiddleware<JwtAuthenticationMiddleware>();
        app.UseAuthorization();
        app.MapControllers();
        app.MapHealthChecks("/health").AllowAnonymous();
        app.MapMcp($"/{McpTransportOptions.EndpointRoute}")
            .RequireAuthorization()
            .RequireCors(McpTransportOptions.CorsPolicyName);

        return app;
    }
}
