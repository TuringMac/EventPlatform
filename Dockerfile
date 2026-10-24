FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG SERVICE
WORKDIR /src
COPY EventPlatform/Directory.Build.props EventPlatform/
COPY EventPlatform/EventPlatform.Common/EventPlatform.Common.csproj EventPlatform/EventPlatform.Common/
COPY EventPlatform/EventPlatform.Contracts/EventPlatform.Contracts.csproj EventPlatform/EventPlatform.Contracts/
COPY EventPlatform/${SERVICE}/${SERVICE}.csproj EventPlatform/${SERVICE}/
RUN dotnet restore "EventPlatform/${SERVICE}/${SERVICE}.csproj"
COPY EventPlatform/ EventPlatform/

FROM build AS publish
ARG SERVICE
RUN dotnet publish "EventPlatform/${SERVICE}/${SERVICE}.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet"]
