var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables(prefix: "KINHUB_");
builder.Services.AddKinHubKinListApi(builder.Configuration, builder.Environment);

var app = builder.Build();
app.UseKinHubKinListApi();

app.Run();
