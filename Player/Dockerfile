FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine AS base
WORKDIR /app
EXPOSE 44360

FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS build
WORKDIR /src
COPY ["./Player/Player.csproj", "Player/"]
COPY ["./Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "Player/Player.csproj"
COPY . .
WORKDIR "/src/Player"
RUN dotnet build "Player.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Player.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Player.dll"]
