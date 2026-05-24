using TourPlanner.DataAccessLayer.Entities;

namespace TourPlanner.DataAccessLayer.Repositories
{
    public interface IUserRepository
    {
        Task<Tour> AddAsync(Tour tour);
        Task<List<Tour>> GetAllUsersAsync();
        Task<Tour?> GetByIdAsync(Guid id);
        Task UpdateAsync(Tour tour);
        Task DeleteAsync(Guid id);
    }
}