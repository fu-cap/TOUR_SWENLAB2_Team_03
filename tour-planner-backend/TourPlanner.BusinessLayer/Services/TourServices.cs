using TourPlanner.DataAccessLayer.Repositories;
using TourPlanner.DataAccessLayer.Entities;
using TourPlanner.DataAccessLayer.Enums;
using TourPlanner.BusinessLayer.Utils;
using TourPlanner.BusinessLayer.Clients;
using TourPlanner.BusinessLayer.Dtos;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using TourPlanner.DataAccessLayer;
using Microsoft.EntityFrameworkCore;

namespace TourPlanner.BusinessLayer.Services
{
    public class TourService: ITourService
    {
        private readonly ITourRepository _tourRepository;
        private readonly ILogRepository _logRepository;
        private readonly HttpClient _httpClient;
        private readonly TourPlannerDbContext? _dbContext;

        private readonly string openRouteAPI = "https://api.openrouteservice.org";

        public TourService(ITourRepository tourRepository, ILogRepository logRepository, HttpClient httpClient, TourPlannerDbContext? dbContext = null)
        {
            _tourRepository = tourRepository;
            _logRepository = logRepository;
            _httpClient = httpClient;
            _dbContext = dbContext;
        }

        public async Task<Tour> CreateTourAsync(CreateTourDto dto)
        {
            // Try to get route info — fall back gracefully if ORS is unavailable (rate limit, timeout, etc.)
            double distanceKm = 0;
            TimeSpan estimatedTime = TimeSpan.Zero;
            string geometryString = string.Empty;

            try
            {
                (distanceKm, estimatedTime, geometryString) = await GetRouteInfoAsync(dto.Waypoints, dto.TransportType);
            }
            catch (HttpRequestException)
            {
                // ORS unavailable — create tour without route data (distance=0, no geometry)
            }
            catch (Exception ex) when (ex.Message.Contains("503") || ex.Message.Contains("unavailable") || ex.Message.Contains("route"))
            {
                // ORS rate-limited or down — create tour without route data
            }

            var tourId = Guid.NewGuid();
            var newTour = new Tour
            {
                Id = tourId,
                UserId = dto.UserId,
                Name = dto.Name,
                Description = dto.Description,
                TransportType = dto.TransportType,
                DistanceKm = distanceKm,
                EstimatedTime = estimatedTime,
                RouteInformation = geometryString,
                CreationDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow,
                Waypoints = dto.Waypoints.Select((w, index) => new Waypoint
                {
                    Id = Guid.NewGuid(),
                    TourId = tourId,
                    OrderIndex = index,
                    Label = w.Label,
                    Latitude = w.Latitude,
                    Longitude = w.Longitude
                }).ToList()
            };

            var (popularity, childFriendliness) = TourMetricsCalculator.Calculate(newTour, new List<Log>());
            newTour.Popularity = popularity;
            newTour.ChildFriendliness = childFriendliness;

            return await _tourRepository.AddAsync(newTour);
        }

        public async Task<List<Tour>> GetAllToursAsync()
        {
            return await _tourRepository.GetAllToursAsync();
        }

        public async Task<List<Tour>> GetToursByUserIdAsync(Guid userId, string? search = null)
        {
            return await _tourRepository.GetToursByUserIdAsync(userId, search);
        }

        public async Task<Tour?> GetTourByIdAsync(Guid id)
        {
            return await _tourRepository.GetByIdAsync(id);
        }

        public async Task UpdateTourAsync(Guid id, CreateTourDto dto)
        {
            var existingTour = await _tourRepository.GetByIdAsync(id);
            if (existingTour == null)
            {
                throw new KeyNotFoundException($"Tour with ID {id} not found.");
            }

            // Try to get route info — fall back gracefully if ORS is unavailable
            double distanceKm = existingTour.DistanceKm;
            TimeSpan estimatedTime = existingTour.EstimatedTime;
            string geometryString = existingTour.RouteInformation;

            try
            {
                (distanceKm, estimatedTime, geometryString) = await GetRouteInfoAsync(dto.Waypoints, dto.TransportType);
            }
            catch (HttpRequestException)
            {
                // ORS unavailable — keep existing route data
            }
            catch (Exception ex) when (ex.Message.Contains("503") || ex.Message.Contains("unavailable"))
            {
                // ORS rate-limited or down — keep existing route data
            }

            existingTour.Name = dto.Name;
            existingTour.Description = dto.Description;
            existingTour.TransportType = dto.TransportType;
            existingTour.DistanceKm = distanceKm;
            existingTour.EstimatedTime = estimatedTime;
            existingTour.RouteInformation = geometryString;
            existingTour.LastModifiedDate = DateTime.UtcNow;
            
            existingTour.Waypoints = dto.Waypoints.Select((w, index) => new Waypoint
            {
                TourId = id,
                OrderIndex = index,
                Label = w.Label,
                Latitude = w.Latitude,
                Longitude = w.Longitude
            }).ToList();

            var logs = await _logRepository.GetLogsByTourIdAsync(id);
            var (popularity, childFriendliness) = TourMetricsCalculator.Calculate(existingTour, logs);
            existingTour.Popularity = popularity;
            existingTour.ChildFriendliness = childFriendliness;

            await _tourRepository.UpdateAsync(existingTour);
        }

        public async Task DeleteTourAsync(Guid id)
        {
            await _tourRepository.DeleteAsync(id);
        }

        private async Task<(double distanceKm, TimeSpan estimatedTime, string geometryString)> GetRouteInfoAsync(List<WaypointDto> waypoints, TransportType transportType)
        {
            string? apiKey = Environment.GetEnvironmentVariable("OpenRoute_ApiKey");

            if (string.IsNullOrEmpty(apiKey))
            {
                throw new Exception("API-Key not found");
            }

            if (waypoints.Count < 2)
            {
                throw new ArgumentException("A tour must have at least a start and an end point.");
            }

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", apiKey);
            
            var requestBody = new OrsRequest
            {
                Coordinates = waypoints.Select(w => new double[] { w.Longitude, w.Latitude }).ToList()
            };

            var response = await _httpClient.PostAsJsonAsync($"{openRouteAPI}/v2/directions/{transportType.ToApiString()}", requestBody);  

            if (response.IsSuccessStatusCode)
            {
                var orsData = await response.Content.ReadFromJsonAsync<OrsResponse>();

                double distanceKm = 0;
                TimeSpan estimatedTime = TimeSpan.Zero;
                string geometryString = "";

                if (orsData?.Routes != null && orsData.Routes.Count > 0)
                {
                    var route = orsData.Routes[0];
                    
                    distanceKm = Math.Round(route.Summary.Distance / 1000.0, 2);
                    estimatedTime = TimeSpan.FromSeconds(route.Summary.Duration);
                    
                    geometryString = route.Geometry; 
                }

                return (distanceKm, estimatedTime, geometryString);
            }
            else
            {
                string errorJson = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Error while getting route information: {errorJson}");
            }
        }

        public async Task<List<TourImportDto>> ExportToursAsync(Guid userId)
        {
            var tours = await _tourRepository.GetToursByUserIdAsync(userId, null);
            var result = new List<TourImportDto>();

            foreach (var tour in tours)
            {
                var logs = await _logRepository.GetLogsByTourIdAsync(tour.Id);
                
                var tourDto = new TourImportDto
                {
                    Name = tour.Name,
                    Description = tour.Description,
                    TransportType = tour.TransportType.ToApiString(),
                    DistanceKm = tour.DistanceKm,
                    EstimatedTime = tour.EstimatedTime,
                    RouteInformation = tour.RouteInformation,
                    Waypoints = tour.Waypoints.OrderBy(w => w.OrderIndex).Select(w => new WaypointImportDto
                    {
                        Label = w.Label,
                        Latitude = w.Latitude,
                        Longitude = w.Longitude
                    }).ToList(),
                    Logs = logs.Select(l => new TourLogImportDto
                    {
                        DateTime = l.DateTime,
                        Comment = l.Comment,
                        Difficulty = l.Difficulty,
                        TotalDistanceKm = l.TotalDistanceKm,
                        TotalTimeMin = l.TotalTimeMin,
                        Rating = l.Rating
                    }).ToList()
                };

                result.Add(tourDto);
            }

            return result;
        }

        public async Task ImportToursAsync(Guid userId, List<TourImportDto> tours)
        {
            if (_dbContext == null)
            {
                throw new InvalidOperationException("Database context is not configured.");
            }

            if (tours == null || tours.Count == 0)
            {
                // Empty import is a no-op — return successfully without error
                return;
            }

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                foreach (var tourDto in tours)
                {
                    // 1. Map Transport Type
                    TransportType parsedTransportType = ParseTransportType(tourDto.TransportType);

                    // 2. Resolve Route Info
                    double distanceKm = tourDto.DistanceKm;
                    TimeSpan estimatedTime = tourDto.EstimatedTime;
                    string routeInfo = tourDto.RouteInformation;

                    bool hasProvidedRoute = !string.IsNullOrWhiteSpace(tourDto.RouteInformation) && 
                                             tourDto.DistanceKm > 0 && 
                                             tourDto.EstimatedTime > TimeSpan.Zero;

                    if (hasProvidedRoute)
                    {
                        distanceKm = tourDto.DistanceKm;
                        estimatedTime = tourDto.EstimatedTime;
                        routeInfo = tourDto.RouteInformation;
                    }
                    else
                    {
                        try
                        {
                            var waypointDtos = tourDto.Waypoints.Select(wp => new WaypointDto
                            {
                                Label = wp.Label,
                                Latitude = wp.Latitude,
                                Longitude = wp.Longitude
                            }).ToList();
                            
                            var (orsDistance, orsTime, orsGeometry) = await GetRouteInfoAsync(waypointDtos, parsedTransportType);
                            distanceKm = orsDistance;
                            estimatedTime = orsTime;
                            routeInfo = orsGeometry;
                        }
                        catch (Exception)
                        {
                            distanceKm = tourDto.DistanceKm;
                            estimatedTime = tourDto.EstimatedTime;
                            routeInfo = tourDto.RouteInformation;
                        }
                    }

                    // 3. Schema Validation
                    if (string.IsNullOrWhiteSpace(tourDto.Name))
                    {
                        throw new ArgumentException("Tour name cannot be empty.");
                    }
                    if (tourDto.Waypoints == null || tourDto.Waypoints.Count < 2)
                    {
                        throw new ArgumentException("A tour must have at least a start and an end point.");
                    }
                    foreach (var wp in tourDto.Waypoints)
                    {
                        if (wp.Latitude < -90 || wp.Latitude > 90)
                        {
                            throw new ArgumentException($"Waypoint latitude '{wp.Latitude}' is out of range (-90 to 90).");
                        }
                        if (wp.Longitude < -180 || wp.Longitude > 180)
                        {
                            throw new ArgumentException($"Waypoint longitude '{wp.Longitude}' is out of range (-180 to 180).");
                        }
                    }
                    if (distanceKm <= 0)
                    {
                        throw new ArgumentException("Tour distance must be positive.");
                    }
                    if (estimatedTime <= TimeSpan.Zero)
                    {
                        throw new ArgumentException("Tour estimated time must be positive.");
                    }

                    // 4. Create Tour Entity
                    var tourId = Guid.NewGuid();
                    var newTour = new Tour
                    {
                        Id = tourId,
                        UserId = userId,
                        Name = tourDto.Name,
                        Description = tourDto.Description ?? string.Empty,
                        TransportType = parsedTransportType,
                        DistanceKm = distanceKm,
                        EstimatedTime = estimatedTime,
                        RouteInformation = routeInfo ?? string.Empty,
                        CreationDate = DateTime.UtcNow,
                        LastModifiedDate = DateTime.UtcNow,
                        Waypoints = tourDto.Waypoints.Select((wp, index) => new Waypoint
                        {
                            Id = Guid.NewGuid(),
                            TourId = tourId,
                            OrderIndex = index,
                            Label = wp.Label ?? string.Empty,
                            Latitude = wp.Latitude,
                            Longitude = wp.Longitude
                        }).ToList()
                    };

                    // Validate logs
                    var logsList = new List<Log>();
                    if (tourDto.Logs != null)
                    {
                        foreach (var logDto in tourDto.Logs)
                        {
                            if (logDto.Difficulty < 1 || logDto.Difficulty > 5)
                            {
                                throw new ArgumentException($"Log difficulty '{logDto.Difficulty}' must be between 1 and 5.");
                            }
                            if (logDto.Rating < 1 || logDto.Rating > 5)
                            {
                                throw new ArgumentException($"Log rating '{logDto.Rating}' must be between 1 and 5.");
                            }
                            if (logDto.TotalDistanceKm <= 0)
                            {
                                throw new ArgumentException("Log total distance must be positive.");
                            }
                            if (logDto.TotalTimeMin <= TimeSpan.Zero)
                            {
                                throw new ArgumentException("Log total time must be positive.");
                            }

                            logsList.Add(new Log
                            {
                                Id = Guid.NewGuid(),
                                TourId = tourId,
                                DateTime = logDto.DateTime,
                                Comment = logDto.Comment ?? string.Empty,
                                Difficulty = logDto.Difficulty,
                                TotalDistanceKm = logDto.TotalDistanceKm,
                                TotalTimeMin = logDto.TotalTimeMin,
                                Rating = logDto.Rating,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            });
                        }
                    }

                    // Recalculate metrics before committing
                    var (popularity, childFriendliness) = TourMetricsCalculator.Calculate(newTour, logsList);
                    newTour.Popularity = popularity;
                    newTour.ChildFriendliness = childFriendliness;
                    // Co2SavedGrams and Co2EmittedGrams are computed properties on Tour entity

                    _dbContext.Tours.Add(newTour);
                    foreach (var log in logsList)
                    {
                        _dbContext.Log.Add(log);
                    }
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private TransportType ParseTransportType(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Transport type is required.");
            }
            
            string clean = value.Trim().ToLowerInvariant();
            
            switch (clean)
            {
                case "bike":
                case "cycling":
                case "bicycle":
                case "cycling-regular":
                case "cyclingregular":
                case "regular-cycling":
                    return TransportType.CyclingRegular;
                    
                case "hike":
                case "hiking":
                case "foot-hiking":
                case "foothiking":
                    return TransportType.FootHiking;
                    
                case "walk":
                case "walking":
                case "foot-walking":
                case "footwalking":
                case "foot-walk":
                    return TransportType.FootWalking;
                    
                case "car":
                case "driving":
                case "driving-car":
                case "automobile":
                case "auto":
                    return TransportType.DrivingCar;
                    
                case "hgv":
                case "truck":
                case "lorry":
                case "driving-hgv":
                    return TransportType.DrivingHgv;
                    
                case "roadbike":
                case "road-bike":
                case "cycling-road":
                case "cyclingroad":
                case "road-cycling":
                    return TransportType.CyclingRoad;
                    
                default:
                    // Fallback: try parsing directly or matching enum string representation
                    if (Enum.TryParse<TransportType>(value, true, out var result))
                    {
                        return result;
                    }
                    throw new ArgumentException($"Unknown transport type: {value}");
            }
        }
    }
}
