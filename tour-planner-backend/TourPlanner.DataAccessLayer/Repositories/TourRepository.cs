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

        public async Task<List<Tour>> GetToursByUserIdAsync(Guid userId, string? search = null)
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                return await _context.Tours
                    .Include(t => t.Waypoints)
                    .Where(t => t.UserId == userId)
                    .OrderByDescending(t => t.CreationDate)
                    .ToListAsync();
            }

            var normalizedSearch = search.Replace(",", ".");
            var escapedNormalizedSearch = normalizedSearch.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
            var likeSearch = $"%{escapedNormalizedSearch}%";

            var tourIds = await _context.Database
                .SqlQuery<Guid>($@"
                    SELECT tour_id 
                    FROM v_tour_search 
                    WHERE user_id = {userId} 
                      AND (
                          search_vector @@ websearch_to_tsquery('english', {search})
                          OR name ILIKE {likeSearch}
                          OR description ILIKE {likeSearch}
                          OR distance_km::text ILIKE {likeSearch}
                          OR estimated_time_min::text ILIKE {likeSearch}
                          OR popularity::text ILIKE {likeSearch}
                          OR child_friendliness::text ILIKE {likeSearch}
                      )")
                .ToListAsync();

            return await _context.Tours
                .Include(t => t.Waypoints)
                .Where(t => tourIds.Contains(t.Id))
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
            // Explicitly mark metrics as modified — EF Core may otherwise skip writing 0.0
            // because HasDefaultValueSql triggers value-generation tracking on numeric columns.
            _context.Entry(existingTour).Property(t => t.Popularity).IsModified = true;
            _context.Entry(existingTour).Property(t => t.ChildFriendliness).IsModified = true;

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

        public async Task UpdateMetricsAsync(Guid tourId, double popularity, double childFriendliness)
        {
            // Use ExecuteUpdateAsync to bypass EF Core's change tracker entirely.
            // This avoids the tracked-waypoint conflict that occurs when UpdateTourMetricsAsync
            // in LogService calls GetByIdAsync before calling UpdateAsync (double-fetch problem).
            await _context.Tours
                .Where(t => t.Id == tourId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(t => t.Popularity, popularity)
                    .SetProperty(t => t.ChildFriendliness, childFriendliness));
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