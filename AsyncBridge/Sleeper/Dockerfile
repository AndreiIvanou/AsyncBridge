FROM microsoft/dotnet:2.1-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY Sleeper/Sleeper.csproj Sleeper/
RUN dotnet restore Sleeper/Sleeper.csproj
COPY . .
WORKDIR /src/Sleeper
RUN dotnet build Sleeper.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish Sleeper.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Sleeper.dll"]
