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

            // Materialize the new waypoints list first to avoid enumeration issues
            var newWaypoints = tour.Waypoints.ToList();

            // Replace waypoints correctly for EF Core
            existingTour.Waypoints.Clear();
            
            foreach (var wp in newWaypoints)
            {
                wp.Id = Guid.NewGuid();
                wp.TourId = tour.Id;
                existingTour.Waypoints.Add(wp);
            }

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