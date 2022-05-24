FROM mcr.microsoft.com/dotnet/aspnet:6.0-bullseye-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 44370

FROM node:16-bullseye-slim AS node_base
FROM mcr.microsoft.com/dotnet/sdk:6.0-bullseye-slim AS dotnet-sdk
COPY --from=node_base . .
RUN echo "NODE Version:" && node --version
RUN echo "NPM Version:" && npm --version

FROM dotnet-sdk as build
WORKDIR /src
COPY ["GameMaster/GameMaster.csproj", "GameMaster/"]
COPY ["Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "GameMaster/GameMaster.csproj"
COPY . .
WORKDIR "/src/GameMaster"
RUN dotnet build "GameMaster.csproj" -c Release -o /app/build

FROM build AS publish
ENV NODE_ENV=production
RUN dotnet publish "GameMaster.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GameMaster.dll"]
