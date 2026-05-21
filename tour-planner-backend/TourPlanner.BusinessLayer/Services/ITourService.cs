using TourPlanner.DataAccessLayer.Entities;
using TourPlanner.DataAccessLayer.Enums;

namespace TourPlanner.BusinessLayer.Services
{
    public interface ITourService
    {
        Task<Tour> CreateTourAsync(int userID, string name, string description, string from, string to, TransportType transportType);
    }
}