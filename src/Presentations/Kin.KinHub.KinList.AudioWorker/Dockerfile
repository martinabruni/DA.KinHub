FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Kin.KinHub.Core.slnx", "./"]
COPY ["src/Businesses/Kin.KinHub.KinList.Business/Kin.KinHub.KinList.Business.csproj", "src/Businesses/Kin.KinHub.KinList.Business/"]
COPY ["src/Domains/Kin.KinHub.KinList.Domain/Kin.KinHub.KinList.Domain.csproj", "src/Domains/Kin.KinHub.KinList.Domain/"]
COPY ["src/Infrastructures/Kin.KinHub.KinList.AzureOpenAi/Kin.KinHub.KinList.AzureOpenAi.csproj", "src/Infrastructures/Kin.KinHub.KinList.AzureOpenAi/"]
COPY ["src/Infrastructures/Kin.KinHub.KinList.AzureStorage/Kin.KinHub.KinList.AzureStorage.csproj", "src/Infrastructures/Kin.KinHub.KinList.AzureStorage/"]
COPY ["src/Infrastructures/Kin.KinHub.KinList.PostgreSql/Kin.KinHub.KinList.PostgreSql.csproj", "src/Infrastructures/Kin.KinHub.KinList.PostgreSql/"]
COPY ["src/Presentations/Kin.KinHub.KinList.AudioWorker/Kin.KinHub.KinList.AudioWorker.csproj", "src/Presentations/Kin.KinHub.KinList.AudioWorker/"]

RUN dotnet restore "src/Presentations/Kin.KinHub.KinList.AudioWorker/Kin.KinHub.KinList.AudioWorker.csproj"

COPY . .
WORKDIR "/src/src/Presentations/Kin.KinHub.KinList.AudioWorker"
RUN dotnet publish "Kin.KinHub.KinList.AudioWorker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Kin.KinHub.KinList.AudioWorker.dll"]
