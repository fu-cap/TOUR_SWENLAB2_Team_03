using Microsoft.EntityFrameworkCore;
using TourPlanner.DataAccessLayer.Entities;

namespace TourPlanner.DataAccessLayer.Repositories
{
    public class TourRepository : ITourRepository
    {
        private readonly TourPlannerDbContext _context;

        public TourRepository(TourPlannerDbContext context)
        {
            _context = context;
        }

        public async Task<Tour> AddAsync(Tour tour)
        {
            _context.Tours.Add(tour);
            await _context.SaveChangesAsync();
            return tour;
        }

        public async Task<List<Tour>> GetAllToursAsync()
        {
            return await _context.Tours
                .Include(t => t.Waypoints)
                .OrderByDescending(t => t.CreationDate)
                .ToListAsync();
        }

        public async Task<Tour?> GetByIdAsync(Guid id)
        {
            return await _context.Tours
                .Include(t => t.Waypoints)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task UpdateAsync(Tour tour)
        {
            var existingTour = await _context.Tours
                .Include(t => t.Waypoints)
                .FirstOrDefaultAsync(t => t.Id == tour.Id);

            if (existingTour == null) return;

            // Update primitive properties
            _context.Entry(existingTour).CurrentValues.SetValues(tour);

            // Replace waypoints: 
            // 1. Remove old ones
            _context.Waypoints.RemoveRange(existingTour.Waypoints);
            
            // 2. Add new ones (ensure they have new IDs and correct TourId)
            foreach (var wp in tour.Waypoints)
            {
                wp.Id = Guid.NewGuid();
                wp.TourId = tour.Id;
            }
            existingTour.Waypoints = tour.Waypoints;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var tour = await _context.Tours.FindAsync(id);
            if (tour != null)
            {
                _context.Tours.Remove(tour);
                await _context.SaveChangesAsync();
            }
        }
    }
}