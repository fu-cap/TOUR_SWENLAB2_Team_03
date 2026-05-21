using TourPlanner.DataAccessLayer.Entities;

namespace TourPlanner.DataAccessLayer.Repositories
{
    public interface ITourRepository
    {
        Task<Tour> AddAsync(Tour tour);
    }
}