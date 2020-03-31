#!/bin/sh

proj="IntegrationTests/IntegrationTests.csproj"

dotnet restore "$proj"
dotnet build --no-restore "$proj"
dotnet test --no-build --no-restore "$proj"
