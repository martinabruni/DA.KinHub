var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables(prefix: "KINHUB_");
builder.Services.AddKinHubSharedApi(builder.Configuration, builder.Environment);

var app = builder.Build();
app.UseKinHubSharedApi();

app.Run();