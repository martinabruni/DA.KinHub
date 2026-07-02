FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Kin.KinHub.Core.slnx", "./"]
COPY ["src/Businesses/Kin.KinHub.KinList.Business/Kin.KinHub.KinList.Business.csproj", "src/Businesses/Kin.KinHub.KinList.Business/"]
COPY ["src/Domains/Kin.KinHub.Core.Domain/Kin.KinHub.Core.Domain.csproj", "src/Domains/Kin.KinHub.Core.Domain/"]
COPY ["src/Domains/Kin.KinHub.KinList.Domain/Kin.KinHub.KinList.Domain.csproj", "src/Domains/Kin.KinHub.KinList.Domain/"]
COPY ["src/Infrastructures/Kin.KinHub.Core.PostgreSql/Kin.KinHub.Core.PostgreSql.csproj", "src/Infrastructures/Kin.KinHub.Core.PostgreSql/"]
COPY ["src/Infrastructures/Kin.KinHub.KinList.PostgreSql/Kin.KinHub.KinList.PostgreSql.csproj", "src/Infrastructures/Kin.KinHub.KinList.PostgreSql/"]
COPY ["src/Presentations/Kin.KinHub.Migrations.Runner/Kin.KinHub.Migrations.Runner.csproj", "src/Presentations/Kin.KinHub.Migrations.Runner/"]

RUN dotnet restore "src/Presentations/Kin.KinHub.Migrations.Runner/Kin.KinHub.Migrations.Runner.csproj"

COPY . .
WORKDIR "/src/src/Presentations/Kin.KinHub.Migrations.Runner"
RUN dotnet publish "Kin.KinHub.Migrations.Runner.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Runs once and exits; non-zero exit code on failure fails the Container Apps Job.
ENTRYPOINT ["dotnet", "Kin.KinHub.Migrations.Runner.dll"]
