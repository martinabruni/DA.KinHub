var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables(prefix: "KINHUB_");
builder.Services.AddKinHubKinRecipeApi(builder.Configuration, builder.Environment);

var app = builder.Build();
app.UseKinHubKinRecipeApi();

app.Run();
