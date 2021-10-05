# FROM mcr.microsoft.com/dotnet/core/sdk:6.0-alpine AS build
FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine as build
WORKDIR /app

# copy csproj and restore as distinct layers
COPY SecretSanta/*.sln ./
COPY SecretSanta/SecretSanta/*.csproj ./SecretSanta/
RUN dotnet restore

# copy everything else and build app
COPY SecretSanta/SecretSanta/. ./SecretSanta/
WORKDIR /app/SecretSanta
RUN dotnet publish -c Release --no-restore -o out


FROM mcr.microsoft.com/dotnet/runtime:6.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app/SecretSanta/out ./
ENTRYPOINT ["dotnet", "SecretSanta.dll"]