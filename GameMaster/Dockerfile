FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine AS base
WORKDIR /app
EXPOSE 80
EXPOSE 44370

# TODO: update node to 18?
FROM node:16-alpine AS node_base
WORKDIR "/client_app"
COPY "GameMaster/ClientApp/package.json" .
COPY "GameMaster/ClientApp/package-lock.json" .
ENV NODE_ENV=production
RUN npm set progress=false && npm config set depth 0
RUN npm install --omit=dev --omit=optional

FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS build
COPY --from=node_base . .

WORKDIR /src
COPY ["GameMaster/GameMaster.csproj", "GameMaster/"]
COPY ["Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "GameMaster/GameMaster.csproj"
COPY . .
WORKDIR "/src/GameMaster"
RUN dotnet build "GameMaster.csproj" -c Release -o /app/build

ENV NODE_ENV=production
WORKDIR "/src/GameMaster/ClientApp"
COPY --from=node_base "/client_app/node_modules" "./node_modules"
RUN npm run build --prod

FROM build AS publish
WORKDIR "/src/GameMaster"
RUN dotnet publish "GameMaster.csproj" -c Release -o /app/publish
RUN mkdir /app/publish/ClientApp
RUN mv ./ClientApp/build /app/publish/ClientApp/build 

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GameMaster.dll"]
