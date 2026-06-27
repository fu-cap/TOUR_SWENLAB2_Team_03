using TourPlanner.DataAccessLayer.Entities;
using TourPlanner.BusinessLayer.Dtos;

namespace TourPlanner.BusinessLayer.Services
{
    public interface IUserService
    {
        Task<User> CreateUserAsync(CreateUserDto createUserDto);
        Task<List<User>> GetAllUsersAsync();
        Task<User?> GetUserByIdAsync(Guid id);
        Task<User?> AuthenticateAsync(string username, string password);
        Task UpdateUserAsync(Guid id, UpdateUserDto updateUserDto);
        Task DeleteUserAsync(Guid id);
    }
}