# Use the official .NET ASP.NET runtime image as the base for the final image.
# We name this stage "base".
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
# Set the working directory inside the container.
WORKDIR /app
# Expose port 80 for the web application.
EXPOSE 80

# Use the official .NET SDK image to build the application.
# We name this stage "build".
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
# Set the working directory inside the container.
WORKDIR /src

# Copy the solution file and project files to the build environment.
# This is a critical step to correctly restore dependencies.
COPY ["Backend_SmartBus.sln", "."]
COPY ["Backend_SmartBus/Backend_SmartBus.csproj", "Backend_SmartBus/"]
COPY ["SmartBus_BusinessObjects/SmartBus_BusinessObjects.csproj", "SmartBus_BusinessObjects/"]

# Run 'dotnet restore' to restore NuGet packages.
RUN dotnet restore "Backend_SmartBus/Backend_SmartBus.csproj"

# Copy the entire source code to the build environment.
COPY . .
# Set the working directory to the specific project folder to publish.
WORKDIR "/src/Backend_SmartBus"
# Publish the application in Release mode.
RUN dotnet publish "Backend_SmartBus.csproj" -c Release -o /app/publish

# Final stage: Use the base runtime image.
FROM base AS final
# Set the working directory inside the container.
WORKDIR /app
# Copy the published output from the build stage to the final image.
COPY --from=build /app/publish .
# Define the command to run when the container starts.
ENTRYPOINT ["dotnet", "Backend_SmartBus.dll"]
