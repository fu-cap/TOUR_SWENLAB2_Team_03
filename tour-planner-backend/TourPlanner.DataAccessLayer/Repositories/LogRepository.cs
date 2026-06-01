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
        public async Task<List<Log>> GetLogsByTourIdAsync(Guid tourId)
        {
            return await _context.Log.Where(log => log.tour_id == tourId).ToListAsync();
        }
        public async Task<Log?> GetByIdAsync(Guid id)
        {
            return await _context.Log.Where(log => log.Id == id).FirstOrDefaultAsync();
        }
        public async Task UpdateAsync(Log Log)
        {
            _context.Log.Update(Log);
            await _context.SaveChangesAsync();
        }
        public async Task DeleteAsync(Guid id)
        {
            var log = await _context.Log.FindAsync(id);
            if (log != null)
            {
                _context.Log.Remove(log);
                await _context.SaveChangesAsync();
            }
        }

    }
}