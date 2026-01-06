FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Install required system dependencies
RUN apt-get update && apt-get install -y \
    libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

# Copy project files for restore
COPY ["KnowledgeSystem.Api/KnowledgeSystem.Api.csproj", "KnowledgeSystem.Api/"]
COPY ["KnowledgeSystem.Infrastructure/KnowledgeSystem.Infrastructure.csproj", "KnowledgeSystem.Infrastructure/"]
COPY ["KnowledgeSystem.Application/KnowledgeSystem.Application.csproj", "KnowledgeSystem.Application/"]
COPY ["KnowledgeSystem.Domain/KnowledgeSystem.Domain.csproj", "KnowledgeSystem.Domain/"]

# Restore dependencies
RUN dotnet restore "KnowledgeSystem.Api/KnowledgeSystem.Api.csproj"

# Copy all source files
COPY . .

# Build and publish
WORKDIR "/src/KnowledgeSystem.Api"
RUN dotnet publish "KnowledgeSystem.Api.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# Install required PostgreSQL libraries for runtime
RUN apt-get update && apt-get install -y \
    libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "KnowledgeSystem.Api.dll"]
