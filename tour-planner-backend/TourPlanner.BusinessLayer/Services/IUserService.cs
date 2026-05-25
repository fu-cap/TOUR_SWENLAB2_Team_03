using TourPlanner.DataAccessLayer.Entities;
using TourPlanner.BusinessLayer.Dtos;

namespace TourPlanner.BusinessLayer.Services
{
    public interface IUserService
    {
        Task<User> CreateUserAsync(CreateUserDto createUserDto);
        Task<List<User>> GetAllUsersAsync();
        Task<User?> GetTourByIdAsync(Guid id);
        Task UpdateUserAsync(Guid id, CreateUserDto updateUserDto);
        Task DeleteUserAsync(Guid id);
    }
}