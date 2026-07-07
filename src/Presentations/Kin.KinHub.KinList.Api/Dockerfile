FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Kin.KinHub.Core.slnx", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["src/Shared/Kin.KinHub.Shared.Kernel/Kin.KinHub.Shared.Kernel.csproj", "src/Shared/Kin.KinHub.Shared.Kernel/"]
COPY ["src/Businesses/Kin.KinHub.Core.Business/Kin.KinHub.Core.Business.csproj", "src/Businesses/Kin.KinHub.Core.Business/"]
COPY ["src/Businesses/Kin.KinHub.Identity.Business/Kin.KinHub.Identity.Business.csproj", "src/Businesses/Kin.KinHub.Identity.Business/"]
COPY ["src/Businesses/Kin.KinHub.KinList.Business/Kin.KinHub.KinList.Business.csproj", "src/Businesses/Kin.KinHub.KinList.Business/"]
COPY ["src/Domains/Kin.KinHub.Core.Domain/Kin.KinHub.Core.Domain.csproj", "src/Domains/Kin.KinHub.Core.Domain/"]
COPY ["src/Domains/Kin.KinHub.Identity.Domain/Kin.KinHub.Identity.Domain.csproj", "src/Domains/Kin.KinHub.Identity.Domain/"]
COPY ["src/Domains/Kin.KinHub.KinList.Domain/Kin.KinHub.KinList.Domain.csproj", "src/Domains/Kin.KinHub.KinList.Domain/"]
COPY ["src/Infrastructures/Kin.KinHub.Identity.Jwt/Kin.KinHub.Identity.Jwt.csproj", "src/Infrastructures/Kin.KinHub.Identity.Jwt/"]
COPY ["src/Infrastructures/Kin.KinHub.KinList.AzureOpenAi/Kin.KinHub.KinList.AzureOpenAi.csproj", "src/Infrastructures/Kin.KinHub.KinList.AzureOpenAi/"]
COPY ["src/Infrastructures/Kin.KinHub.KinList.AzureStorage/Kin.KinHub.KinList.AzureStorage.csproj", "src/Infrastructures/Kin.KinHub.KinList.AzureStorage/"]
COPY ["src/Infrastructures/Kin.KinHub.KinList.PostgreSql/Kin.KinHub.KinList.PostgreSql.csproj", "src/Infrastructures/Kin.KinHub.KinList.PostgreSql/"]
COPY ["src/Presentations/Kin.KinHub.Core.Api/Kin.KinHub.Core.Api.csproj", "src/Presentations/Kin.KinHub.Core.Api/"]
COPY ["src/Presentations/Kin.KinHub.KinList.Api/Kin.KinHub.KinList.Api.csproj", "src/Presentations/Kin.KinHub.KinList.Api/"]

RUN dotnet restore "src/Presentations/Kin.KinHub.KinList.Api/Kin.KinHub.KinList.Api.csproj"

COPY . .
WORKDIR "/src/src/Presentations/Kin.KinHub.KinList.Api"
RUN dotnet publish "Kin.KinHub.KinList.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Kin.KinHub.KinList.Api.dll"]
