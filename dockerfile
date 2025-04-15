# --- Build Stage ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copy project files and restore dependencies
COPY Final/Final.csproj Final/
RUN dotnet restore "Final/Final.csproj"

# Copy the full source code and build
COPY . .
WORKDIR "/src/Final"
RUN dotnet publish "Final.csproj" -c Release -o /app/publish

# --- Runtime Stage ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app
COPY --from=build /app/publish .

# Expose the port that Kestrel listens on
EXPOSE 5000

# Run the application
ENTRYPOINT ["dotnet", "Final.dll"]
