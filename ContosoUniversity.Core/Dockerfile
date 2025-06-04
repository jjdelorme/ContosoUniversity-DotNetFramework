# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["ContosoUniversity.Core.csproj", "./"]
RUN dotnet restore "ContosoUniversity.Core.csproj"
COPY . .
RUN dotnet build "ContosoUniversity.Core.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "ContosoUniversity.Core.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Copy published files
COPY --from=publish /app/publish .

# Create non-root user
RUN adduser --disabled-password --home /app --gecos '' appuser && chown -R appuser /app
USER appuser

ENTRYPOINT ["dotnet", "ContosoUniversity.Core.dll"]