FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore as distinct layers
COPY ["TourPlanner.API/TourPlanner.API.csproj", "TourPlanner.API/"]
COPY ["TourPlanner.BusinessLayer/TourPlanner.BusinessLayer.csproj", "TourPlanner.BusinessLayer/"]
COPY ["TourPlanner.DataAccessLayer/TourPlanner.DataAccessLayer.csproj", "TourPlanner.DataAccessLayer/"]
COPY ["TourPlanner.Tests/TourPlanner.Tests.csproj", "TourPlanner.Tests/"]

RUN dotnet restore "TourPlanner.API/TourPlanner.API.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/TourPlanner.API"
RUN dotnet build "TourPlanner.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TourPlanner.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TourPlanner.API.dll"]
