#!/bin/sh
projects=(Shared GameMaster CommunicationServer Player)

# Build
for proj in "${projects[@]}"; do 
	dotnet restore "$proj/${proj}.csproj";
	dotnet build --no-restore "$proj/${proj}.csproj"
	
	dotnet restore "${proj}Tests/${proj}.Tests.csproj"
	dotnet build --no-restore "${proj}Tests/${proj}.Tests.csproj"
done

# Tests
for proj in "${projects[@]}"; do 
	dotnet test --no-build --no-restore "${proj}Tests/${proj}.Tests.csproj"
done