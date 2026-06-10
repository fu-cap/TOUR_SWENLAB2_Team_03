using TourPlanner.DataAccessLayer.Entities;

namespace TourPlanner.DataAccessLayer.Repositories
{
    public interface ITourRepository
    {
        Task<Tour> AddAsync(Tour tour);
        Task<List<Tour>> GetAllToursAsync();
        Task<List<Tour>> GetToursByUserIdAsync(Guid userId);
        Task<Tour?> GetByIdAsync(Guid id);
        Task UpdateAsync(Tour tour);
        Task DeleteAsync(Guid id);
    }
}