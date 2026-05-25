using TourPlanner.BusinessLayer.Dtos;
using TourPlanner.DataAccessLayer.Entities;
using TourPlanner.DataAccessLayer.Repositories;

namespace TourPlanner.BusinessLayer.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository userRepository;

        public UserService(IUserRepository userRepository)
        {
            this.userRepository = userRepository;
        }

        public async Task<User> CreateUserAsync(CreateUserDto createUserDto)
        {    
            throw new NotImplementedException("CreateUserAsync not implemented");
        }
        
        public async Task<List<User>> GetAllUsersAsync()
        {
            return await userRepository.GetAllUsersAsync();
        }

        public async Task<User?> GetTourByIdAsync(Guid id)
        {
            return null;
        }

        public async Task UpdateUserAsync(Guid id, CreateUserDto updateUserDto)
        {
            
        }

        public async Task DeleteUserAsync(Guid id)
        {
            
        }
    }
}