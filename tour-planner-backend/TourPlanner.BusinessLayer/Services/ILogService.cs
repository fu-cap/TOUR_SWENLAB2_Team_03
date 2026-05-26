using TourPlanner.DataAccessLayer.Entities;
using TourPlanner.BusinessLayer.Dtos;

namespace TourPlanner.BusinessLayer.Services
{
    public interface ILogService
    {
        Task<Log> CreateLogAsync(CreateLogDto createLogDto);
        Task<List<Log>> GetAllLogsAsync();
        Task<Log?> GetLogByIdAsync(Guid id);
        Task UpdateLogAsync(Guid id, CreateLogDto updateLogDto);
        Task DeleteLogAsync(Guid id);
    }
}