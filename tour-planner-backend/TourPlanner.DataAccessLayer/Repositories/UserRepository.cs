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

        public async Task<User> AddAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }
        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users
                .ToListAsync();
        }
        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _context.Users
                .FirstOrDefaultAsync(t => t.Id == id);
        }
        public async Task UpdateAsync(User user)
        {
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(t => t.Id == user.Id);

            if (existingUser == null) return;

            _context.Entry(existingUser).CurrentValues.SetValues(user);

            await _context.SaveChangesAsync();
        }
        public async Task DeleteAsync(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
        }
    }
}