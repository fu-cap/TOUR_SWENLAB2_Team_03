FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src
COPY ["TourPlannerAPI/TourPlannerAPI.csproj", "TourPlannerAPI/"]
RUN dotnet restore "TourPlannerAPI/TourPlannerAPI.csproj"
COPY . .
WORKDIR "/src/TourPlannerAPI"
RUN dotnet build "TourPlannerAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TourPlannerAPI.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TourPlannerAPI.dll"]
