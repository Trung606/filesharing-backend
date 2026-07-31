# Stage 1: Build the application using the .NET 9 SDK
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# 1. REVERTED: Paths are back to targeting the current directory directly
COPY ["FileSharingAPI.csproj", "./"]
RUN dotnet restore "FileSharingAPI.csproj"

# Copy all remaining source code files
COPY . .

# 2. REVERTED: Build from the current directory context
WORKDIR "/src/."
RUN dotnet build "FileSharingAPI.csproj" -c Release -o /app/build

# Stage 2: Publish the application
FROM build AS publish
RUN dotnet publish "FileSharingAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Final runtime image (lightweight)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Copy published files from the build stage
COPY --from=publish /app/publish .

# Set the entry point to run your compiled API
ENTRYPOINT ["dotnet", "FileSharingAPI.dll"]