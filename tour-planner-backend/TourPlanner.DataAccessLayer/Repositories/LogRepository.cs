using Microsoft.EntityFrameworkCore;
using TourPlanner.DataAccessLayer.Entities;

namespace TourPlanner.DataAccessLayer.Repositories
{
    public class LogRepository : ILogRepository
    {
        private readonly TourPlannerDbContext _context;

        public LogRepository(TourPlannerDbContext context)
        {
            _context = context;
        }
        public async Task<Log> AddAsync(Log Log)
        {
            _context.Log.Add(Log);
            await _context.SaveChangesAsync();
            return Log;
        }
        public async Task<List<Log>> GetAllLogsAsync()
        {
            return await _context.Log.ToListAsync();
        }
        public async Task<Log?> GetByIdAsync(Guid id)
        {
            throw new NotImplementedException();
        }
        public async Task UpdateAsync(Log Log)
        {
            throw new NotImplementedException();
        }
        public async Task DeleteAsync(Guid id)
        {
            throw new NotImplementedException();
        }

    }
}