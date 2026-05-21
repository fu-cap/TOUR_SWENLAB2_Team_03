using TourPlanner.DataAccessLayer.Repositories;
using TourPlanner.DataAccessLayer.Entities;
using TourPlanner.DataAccessLayer.Enums;
using TourPlanner.BusinessLayer.Utils;

namespace TourPlanner.BusinessLayer.Services
{
    public class TourService: ITourService
    {
        private readonly ITourRepository _tourRepository;

        public TourService(ITourRepository tourRepository)
        {
            _tourRepository = tourRepository;
        }

        public async Task<Tour> CreateTourAsync(int userID, string name, string description, string from, string to, TransportType transportType)
        {
            double distance_km = 0; // Placeholder, should be calculated based on 'from' and 'to'
            TimeSpan estimatedTime = TimeSpan.Zero; // Placeholder, should be calculated based on distance and transport type

            var newTour = new Tour
            {
                userID = userID,
                Name = name,
                Description = description,
                From = from,
                To = to,
                TransportType = transportType,
                Distance_km = distance_km,
                EstimatedTime = estimatedTime,
                RouteInformation = "placeholder_map.png", // Placeholder, should be set after map generation
                CreationDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };

            return await _tourRepository.AddAsync(newTour);
        }

        public async Task<List<Tour>> GetAllToursAsync()
        {
            return await _tourRepository.GetAllToursAsync();
        }
    }
}