using TourPlanner.DataAccessLayer.Repositories;
using TourPlanner.DataAccessLayer.Entities;
using TourPlanner.DataAccessLayer.Enums;
using TourPlanner.BusinessLayer.Utils;
using TourPlanner.BusinessLayer.Clients;
using TourPlanner.API.Dtos;
using System.Net.Http.Json;
using System.Net.Http.Headers;


namespace TourPlanner.BusinessLayer.Services
{
    public class TourService: ITourService
    {
        private readonly ITourRepository _tourRepository;

        private readonly string OpenRouteAPI = "https://api.openrouteservice.org";

        public TourService(ITourRepository tourRepository)
        {
            _tourRepository = tourRepository;
        }

        private static HttpClient openrouteClient = new();

        public async Task<Tour> CreateTourAsync(CreateTourDto dto)
        {
            string? apiKey = Environment.GetEnvironmentVariable("OpenRoute_ApiKey");


            if (string.IsNullOrEmpty(apiKey))
            {
                throw new Exception("API-Key not found");
            }

            if (dto.waypoints.Count < 2)
            {
                throw new ArgumentException("A tour must have at least a start and an end point.");
            }

            openrouteClient.DefaultRequestHeaders.Clear();
            openrouteClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", apiKey);
            
            var requestBody = new OrsRequest
            {
                Coordinates = dto.waypoints.Select(w => new double[] { w.longitude, w.latitude }).ToList()
            };

            var response = await openrouteClient.PostAsJsonAsync($"{OpenRouteAPI}/v2/directions/{dto.TransportType.ToApiString()}", requestBody);  

            if (response.IsSuccessStatusCode)
            {
                var orsData = await response.Content.ReadFromJsonAsync<OrsResponse>();

                double distance_km = 0;
                TimeSpan estimatedTime = TimeSpan.Zero;
                string geometryString = "";

                if (orsData?.Routes != null && orsData.Routes.Count > 0)
                {
                    var route = orsData.Routes[0];
                    
                    distance_km = Math.Round(route.Summary.Distance / 1000.0, 2);
                    estimatedTime = TimeSpan.FromSeconds(route.Summary.Duration);
                    
                    geometryString = route.Geometry; 
                }

                var newTour = new Tour
                {
                    userID = dto.userId,
                    Name = dto.name,
                    Description = dto.description,
                    TransportType = dto.TransportType,
                    Distance_km = distance_km,
                    EstimatedTime = estimatedTime,
                    RouteInformation = geometryString,
                    CreationDate = DateTime.UtcNow,
                    LastModifiedDate = DateTime.UtcNow,
                    Waypoints = dto.waypoints.Select((w, index) => new Waypoint
                    {
                        OrderIndex = index,
                        Address = w.address,
                        Latitude = w.latitude,
                        Longitude = w.longitude
                    }).ToList()
                };

                return await _tourRepository.AddAsync(newTour);
            }
            else
            {
                string errorJson = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Error while getting route information: {errorJson}");
            }

        }

        public async Task<List<Tour>> GetAllToursAsync()
        {
            return await _tourRepository.GetAllToursAsync();
        }
    }
}