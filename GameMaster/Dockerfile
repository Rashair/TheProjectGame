FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine AS base
WORKDIR /app
EXPOSE 80
EXPOSE 44370

# TODO: update node to 18?
FROM node:16-alpine AS node_build
WORKDIR "client_app"
COPY "GameMaster/ClientApp/package.json" .
COPY "GameMaster/ClientApp/package-lock.json" .
ENV NODE_ENV=production
RUN npm set progress=false && npm config set depth 0
RUN npm install --omit=dev --omit=optional
COPY "GameMaster/ClientApp/" .
RUN npm run build --prod

FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS dotnet_build
WORKDIR /src
COPY ["GameMaster/GameMaster.csproj", "GameMaster/"]
COPY ["Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "GameMaster/GameMaster.csproj"
COPY . .
WORKDIR "/src/GameMaster"
RUN dotnet build "GameMaster.csproj" -c Release -o /app/build

FROM dotnet_build AS publish
WORKDIR "/src/GameMaster"
RUN dotnet publish "GameMaster.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=node_build /client_app/build ./ClientApp/build

ENTRYPOINT ["dotnet", "GameMaster.dll"]
