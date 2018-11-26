FROM microsoft/dotnet:2.1-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY Counter/Counter.csproj Counter/
RUN dotnet restore Counter/Counter.csproj
COPY . .
WORKDIR /src/Counter
RUN dotnet build Counter.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish Counter.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Counter.dll"]
