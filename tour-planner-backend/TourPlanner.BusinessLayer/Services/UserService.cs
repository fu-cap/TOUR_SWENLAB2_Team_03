using TourPlanner.BusinessLayer.Dtos;
using TourPlanner.DataAccessLayer.Entities;
using TourPlanner.DataAccessLayer.Repositories;
using TourPlanner.BusinessLayer.Utils;

namespace TourPlanner.BusinessLayer.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<User> CreateUserAsync(CreateUserDto createUserDto)
        {    
            var newUser = new User
            {
                Username = createUserDto.Username,
                PasswordHash = HashUtil.HashPassword(createUserDto.Password),
                Email = createUserDto.Email,
                Gender = createUserDto.Gender,
                FirstName = createUserDto.Firstname,
                LastName = createUserDto.Lastname,
                CreatedAt = DateTime.UtcNow
            };

            return await _userRepository.AddAsync(newUser);
        }
        
        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _userRepository.GetAllUsersAsync();
        }

        public async Task<User?> GetUserByIdAsync(Guid id)
        {
            return await _userRepository.GetByIdAsync(id);
        }

        public async Task UpdateUserAsync(Guid id, CreateUserDto updateUserDto)
        {
            // Implementation for update
        }

        public async Task DeleteUserAsync(Guid id)
        {
            await _userRepository.DeleteAsync(id);
        }
    }
}
