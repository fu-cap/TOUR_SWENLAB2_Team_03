using Microsoft.EntityFrameworkCore;
using TourPlanner.DataAccessLayer.Entities;

namespace TourPlanner.DataAccessLayer.Repositories
{
    public class UserRepository: IUserRepository
    {
        private readonly TourPlannerDbContext _context;

        public UserRepository(TourPlannerDbContext context)
        {
            _context = context;
        }

        public async Task<Tour> AddAsync(Tour tour)
        {
            return tour;
        }
        public async Task<List<Tour>> GetAllUsersAsync()
        {
            List<Tour> tour = [];
            return tour;
        }
        public async Task<Tour?> GetByIdAsync(Guid id)
        {
            return null;
        }
        public async Task UpdateAsync(Tour tour)
        {
            
        }
        public async Task DeleteAsync(Guid id)
        {
            
        }
    }
}