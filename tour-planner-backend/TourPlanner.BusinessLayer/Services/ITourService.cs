using TourPlanner.DataAccessLayer.Entities;
using TourPlanner.DataAccessLayer.Enums;

namespace TourPlanner.BusinessLayer.Services
{
    public interface ITourService
    {
        Task<Tour> CreateTourAsync(Guid userID, string name, string description, double[] from, double[] to, TransportType transportType);
        Task<List<Tour>> GetAllToursAsync();
    }
}