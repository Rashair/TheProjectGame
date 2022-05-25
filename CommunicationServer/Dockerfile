FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine AS base
WORKDIR /app
EXPOSE 44360
EXPOSE 44370

FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS build
WORKDIR /src
COPY ["CommunicationServer/CommunicationServer.csproj", "CommunicationServer/"]
COPY ["Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "CommunicationServer/CommunicationServer.csproj"
COPY . .
WORKDIR "/src/CommunicationServer"
RUN dotnet build "CommunicationServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CommunicationServer.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CommunicationServer.dll"]
