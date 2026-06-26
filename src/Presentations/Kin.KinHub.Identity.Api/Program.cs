var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables(prefix: "KINHUB_");
builder.Services.AddKinHubIdentityApi(builder.Configuration, builder.Environment);

var app = builder.Build();
app.UseKinHubIdentityApi();

app.Run();
