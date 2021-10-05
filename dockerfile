# FROM mcr.microsoft.com/dotnet/core/sdk:3.0 AS build
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 as build
WORKDIR /app

# copy csproj and restore as distinct layers
COPY *.sln .
COPY SecretSanta3/*.csproj ./SecretSanta3/
RUN dotnet restore

# copy everything else and build app
COPY SecretSanta3/. ./SecretSanta3/
WORKDIR /app/SecretSanta3
RUN dotnet publish -c Release -o out


FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS runtime
WORKDIR /app
COPY --from=build /app/SecretSanta3/out ./
ENTRYPOINT ["dotnet", "SecretSanta3.dll"]