using TourPlanner.DataAccessLayer.Entities;

namespace TourPlanner.DataAccessLayer.Repositories
{
    public interface IUserRepository
    {
        Task<User> AddAsync(User user);
        Task<List<User>> GetAllUsersAsync();
        Task<User?> GetByIdAsync(Guid id);
        Task<User?> GetByUsernameAsync(string username);
        Task UpdateAsync(User user);
        Task DeleteAsync(Guid id);
    }
}