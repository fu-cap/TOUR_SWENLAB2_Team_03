using TourPlanner.DataAccessLayer.Entities;

namespace TourPlanner.DataAccessLayer.Repositories
{
    public interface ITourRepository
    {
        Task<Tour> AddAsync(Tour tour);
        Task<List<Tour>> GetAllToursAsync();
        Task<List<Tour>> GetToursByUserIdAsync(Guid userId, string? search = null);
        Task<Tour?> GetByIdAsync(Guid id);
        Task UpdateAsync(Tour tour);
        /// <summary>Update only Popularity and ChildFriendliness (avoids waypoint tracking conflicts).</summary>
        Task UpdateMetricsAsync(Guid tourId, double popularity, double childFriendliness);
        Task DeleteAsync(Guid id);
    }
}