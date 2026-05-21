using TourPlanner.DataAccessLayer.Entities;

namespace TourPlanner.DataAccessLayer.Repositories
{
    public class TourRepository : ITourRepository
    {
        // In-Memory Implementation bis Datenbank erstellt ist
        private static readonly List<Tour> _tours = new();
        private static int _counter = 1;

        public Task<Tour> AddAsync(Tour tour)
        {
            tour.Id = _counter++;
            _tours.Add(tour);
            return Task.FromResult(tour);
        }

        public Task<List<Tour>> GetAllToursAsync()
        {
            return Task.FromResult(_tours);
        }
    }
}