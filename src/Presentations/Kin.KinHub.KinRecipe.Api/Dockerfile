FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Kin.KinHub.Core.slnx", "./"]
COPY ["src/Businesses/Kin.KinHub.Core.Business/Kin.KinHub.Core.Business.csproj", "src/Businesses/Kin.KinHub.Core.Business/"]
COPY ["src/Businesses/Kin.KinHub.KinRecipe.Business/Kin.KinHub.KinRecipe.Business.csproj", "src/Businesses/Kin.KinHub.KinRecipe.Business/"]
COPY ["src/Businesses/Kin.KinHub.Identity.Business/Kin.KinHub.Identity.Business.csproj", "src/Businesses/Kin.KinHub.Identity.Business/"]
COPY ["src/Domains/Kin.KinHub.Core.Domain/Kin.KinHub.Core.Domain.csproj", "src/Domains/Kin.KinHub.Core.Domain/"]
COPY ["src/Domains/Kin.KinHub.KinRecipe.Domain/Kin.KinHub.KinRecipe.Domain.csproj", "src/Domains/Kin.KinHub.KinRecipe.Domain/"]
COPY ["src/Domains/Kin.KinHub.Identity.Domain/Kin.KinHub.Identity.Domain.csproj", "src/Domains/Kin.KinHub.Identity.Domain/"]
COPY ["src/Infrastructures/Kin.KinHub.KinRecipe.AzureOpenAi/Kin.KinHub.KinRecipe.AzureOpenAi.csproj", "src/Infrastructures/Kin.KinHub.KinRecipe.AzureOpenAi/"]
COPY ["src/Infrastructures/Kin.KinHub.Core.PostgreSql/Kin.KinHub.Core.PostgreSql.csproj", "src/Infrastructures/Kin.KinHub.Core.PostgreSql/"]
COPY ["src/Infrastructures/Kin.KinHub.KinRecipe.PostgreSql/Kin.KinHub.KinRecipe.PostgreSql.csproj", "src/Infrastructures/Kin.KinHub.KinRecipe.PostgreSql/"]
COPY ["src/Infrastructures/Kin.KinHub.Identity.Jwt/Kin.KinHub.Identity.Jwt.csproj", "src/Infrastructures/Kin.KinHub.Identity.Jwt/"]
COPY ["src/Presentations/Kin.KinHub.Core.Api/Kin.KinHub.Core.Api.csproj", "src/Presentations/Kin.KinHub.Core.Api/"]
COPY ["src/Presentations/Kin.KinHub.KinRecipe.Api/Kin.KinHub.KinRecipe.Api.csproj", "src/Presentations/Kin.KinHub.KinRecipe.Api/"]

RUN dotnet restore "src/Presentations/Kin.KinHub.KinRecipe.Api/Kin.KinHub.KinRecipe.Api.csproj"

COPY . .
WORKDIR "/src/src/Presentations/Kin.KinHub.KinRecipe.Api"
RUN dotnet publish "Kin.KinHub.KinRecipe.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Kin.KinHub.KinRecipe.Api.dll"]
