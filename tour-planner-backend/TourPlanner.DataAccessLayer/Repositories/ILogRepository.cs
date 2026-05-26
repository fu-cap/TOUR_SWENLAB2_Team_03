using TourPlanner.DataAccessLayer.Entities;

namespace TourPlanner.DataAccessLayer.Repositories
{
    public interface ILogRepository
    {
        Task<Log> AddAsync(Log Log);
        Task<List<Log>> GetAllLogsAsync();
        Task<Log?> GetByIdAsync(Guid id);
        Task UpdateAsync(Log Log);
        Task DeleteAsync(Guid id);
    }
}