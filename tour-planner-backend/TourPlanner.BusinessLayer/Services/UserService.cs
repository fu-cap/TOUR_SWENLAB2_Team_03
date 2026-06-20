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
                username = createUserDto.username,
                password_hash = HashUtil.HashPassword(createUserDto.password),
                email = createUserDto.email,
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
            var user = await _userRepository.GetByIdAsync(id);
            if(user == null)
            {
                throw new KeyNotFoundException("User not found");
            }

            user.username = updateUserDto.username; 
            user.password_hash = HashUtil.HashPassword(updateUserDto.password);
            user.email = updateUserDto.email;

            await _userRepository.UpdateAsync(user);
        }

        public async Task DeleteUserAsync(Guid id)
        {
            await _userRepository.DeleteAsync(id);
        }
    }
}