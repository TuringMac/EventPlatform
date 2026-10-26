FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG SERVICE
WORKDIR /src
COPY EventPlatform/Directory.Build.props EventPlatform/
COPY EventPlatform/Common/EventPlatform.Common.Api/EventPlatform.Common.Api.csproj EventPlatform/Common/EventPlatform.Common.Api/
COPY EventPlatform/Common/EventPlatform.Common.Application/EventPlatform.Common.Application.csproj EventPlatform/Common/EventPlatform.Common.Application/
COPY EventPlatform/Common/EventPlatform.Common.Domain/EventPlatform.Common.Domain.csproj EventPlatform/Common/EventPlatform.Common.Domain/
COPY EventPlatform/Common/EventPlatform.Common.Infrastructure/EventPlatform.Common.Infrastructure.csproj EventPlatform/Common/EventPlatform.Common.Infrastructure/
COPY EventPlatform/EventPlatform.Contracts/EventPlatform.Contracts.csproj EventPlatform/EventPlatform.Contracts/
COPY EventPlatform/${SERVICE}/EventPlatform.${SERVICE}.Api/EventPlatform.${SERVICE}.Api.csproj EventPlatform/${SERVICE}/EventPlatform.${SERVICE}.Api/
COPY EventPlatform/${SERVICE}/EventPlatform.${SERVICE}.Application/EventPlatform.${SERVICE}.Application.csproj EventPlatform/${SERVICE}/EventPlatform.${SERVICE}.Application/
COPY EventPlatform/${SERVICE}/EventPlatform.${SERVICE}.Domain/EventPlatform.${SERVICE}.Domain.csproj EventPlatform/${SERVICE}/EventPlatform.${SERVICE}.Domain/
COPY EventPlatform/${SERVICE}/EventPlatform.${SERVICE}.Infrastructure/EventPlatform.${SERVICE}.Infrastructure.csproj EventPlatform/${SERVICE}/EventPlatform.${SERVICE}.Infrastructure/
RUN dotnet restore "EventPlatform/${SERVICE}/EventPlatform.${SERVICE}.Api/EventPlatform.${SERVICE}.Api.csproj"
COPY EventPlatform/ EventPlatform/

FROM build AS publish
ARG SERVICE
RUN dotnet publish "EventPlatform/${SERVICE}/EventPlatform.${SERVICE}.Api/EventPlatform.${SERVICE}.Api.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet"]
