# ===============
# BUILD IMAGE
# ===============
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /app

# Copy csproj and restore as distinct layers

WORKDIR /app/SteamUserOperator
COPY ./SteamUserOperator/*.csproj ./
RUN dotnet restore

# Copy everything else and build
WORKDIR /app
COPY ./SteamUserOperator/ ./SteamUserOperator


RUN dotnet publish SteamUserOperator/ -c Release -o out


# ===============
# RUNTIME IMAGE
# ===============
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS runtime
WORKDIR /app

COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "SteamUserOperator.dll"]
