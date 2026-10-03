# Build from the repository root (the API references GymBook.Shared):
#   docker build -f GymBook.Api/Dockerfile .
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

# Npgsql loads GSSAPI for Kerberos auth; without it startup logs a missing-library error.
RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["GymBook.Api/GymBook.Api.csproj", "GymBook.Api/"]
COPY ["GymBook.Shared/GymBook.Shared.csproj", "GymBook.Shared/"]
RUN dotnet restore "GymBook.Api/GymBook.Api.csproj"

COPY GymBook.Api/ GymBook.Api/
COPY GymBook.Shared/ GymBook.Shared/
# The exercise library is compiled in from the app (see GymBook.Api.csproj).
COPY GymBook/Src/Services/Exercises/ GymBook/Src/Services/Exercises/
COPY GymBook/Src/Models/ExerciseDetails.cs GymBook/Src/Models/
RUN dotnet publish "GymBook.Api/GymBook.Api.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "GymBook.Api.dll"]
