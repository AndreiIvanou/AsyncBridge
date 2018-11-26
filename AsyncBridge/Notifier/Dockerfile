FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY Notifier/Notifier.csproj Notifier/
RUN dotnet restore Notifier/Notifier.csproj
COPY . .
WORKDIR /src/Notifier
RUN dotnet build Notifier.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish Notifier.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Notifier.dll"]
