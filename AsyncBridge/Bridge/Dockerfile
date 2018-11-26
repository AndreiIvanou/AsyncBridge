FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY Bridge/Dispatcher.csproj Bridge/
RUN dotnet restore Bridge/Dispatcher.csproj
COPY . .
WORKDIR /src/Bridge
RUN dotnet build Dispatcher.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish Dispatcher.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Dispatcher.dll"]
