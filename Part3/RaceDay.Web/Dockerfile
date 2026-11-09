# ---------- Stage 1: build the website ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy only the project file first, so the package restore layer is cached between builds
COPY RaceDay.Web.csproj ./
RUN dotnet restore RaceDay.Web.csproj

# Now copy the rest of the code and publish
COPY . .
RUN dotnet publish RaceDay.Web.csproj -c Release -o /app/publish --no-restore

# ---------- Stage 2: the small image that actually runs ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .

# Do not run as root
USER app

ENTRYPOINT ["dotnet", "RaceDay.Web.dll"]
