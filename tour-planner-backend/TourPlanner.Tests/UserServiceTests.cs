using Moq;
using TourPlanner.BusinessLayer.Dtos;
using TourPlanner.BusinessLayer.Services;
using TourPlanner.BusinessLayer.Utils;
using TourPlanner.DataAccessLayer.Entities;
using TourPlanner.DataAccessLayer.Repositories;

namespace TourPlanner.Tests
{
    [TestFixture]
    public class UserServiceTests
    {
        private Mock<IUserRepository> _userRepositoryMock = null!;
        private UserService _userService = null!;

        [SetUp]
        public void SetUp()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _userService = new UserService(_userRepositoryMock.Object);
        }

        [Test]
        public async Task CreateUserAsync_ShouldHashPasswordAndSaveUser()
        {
            // Arrange
            var dto = new CreateUserDto
            {
                Username = "testuser",
                Password = "securepassword",
                Email = "test@example.com",
                Gender = "Other",
                Firstname = "Test",
                Lastname = "User"
            };

            User? capturedUser = null;
            _userRepositoryMock
                .Setup(repo => repo.AddAsync(It.IsAny<User>()))
                .Callback<User>(u => capturedUser = u)
                .ReturnsAsync((User u) => u);

            // Act
            var result = await _userService.CreateUserAsync(dto);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(capturedUser, Is.Not.Null);
            Assert.That(capturedUser!.Username, Is.EqualTo(dto.Username));
            Assert.That(capturedUser.PasswordHash, Is.Not.EqualTo(dto.Password));
            Assert.That(HashUtil.CheckPassword(capturedUser.PasswordHash, dto.Password), Is.True);
            _userRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<User>()), Times.Once);
        }

        [Test]
        public async Task GetAllUsersAsync_ShouldReturnAllUsersFromRepository()
        {
            // Arrange
            var expectedUsers = new List<User> { new User { Username = "user1" } };
            _userRepositoryMock.Setup(repo => repo.GetAllUsersAsync()).ReturnsAsync(expectedUsers);

            // Act
            var result = await _userService.GetAllUsersAsync();

            // Assert
            Assert.That(result, Is.EqualTo(expectedUsers));
            _userRepositoryMock.Verify(repo => repo.GetAllUsersAsync(), Times.Once);
        }

        [Test]
        public async Task GetUserByIdAsync_ShouldReturnUserFromRepository()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expectedUser = new User { Id = userId, Username = "user1" };
            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(userId)).ReturnsAsync(expectedUser);

            // Act
            var result = await _userService.GetUserByIdAsync(userId);

            // Assert
            Assert.That(result, Is.EqualTo(expectedUser));
            _userRepositoryMock.Verify(repo => repo.GetByIdAsync(userId), Times.Once);
        }

        [Test]
        public async Task AuthenticateAsync_UserNotFound_ShouldReturnNull()
        {
            // Arrange
            _userRepositoryMock.Setup(repo => repo.GetByUsernameAsync("nonexistent")).ReturnsAsync((User?)null);

            // Act
            var result = await _userService.AuthenticateAsync("nonexistent", "password");

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task AuthenticateAsync_WrongPassword_ShouldReturnNull()
        {
            // Arrange
            var user = new User
            {
                Username = "testuser",
                PasswordHash = HashUtil.HashPassword("correctpassword")
            };
            _userRepositoryMock.Setup(repo => repo.GetByUsernameAsync("testuser")).ReturnsAsync(user);

            // Act
            var result = await _userService.AuthenticateAsync("testuser", "wrongpassword");

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task AuthenticateAsync_CorrectPassword_ShouldReturnUser()
        {
            // Arrange
            var user = new User
            {
                Username = "testuser",
                PasswordHash = HashUtil.HashPassword("correctpassword")
            };
            _userRepositoryMock.Setup(repo => repo.GetByUsernameAsync("testuser")).ReturnsAsync(user);

            // Act
            var result = await _userService.AuthenticateAsync("testuser", "correctpassword");

            // Assert
            Assert.That(result, Is.EqualTo(user));
        }

        [Test]
        public async Task DeleteUserAsync_ShouldCallRepositoryDelete()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var existingUser = new User { Id = userId, Username = "toDelete" };
            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(userId)).ReturnsAsync(existingUser);
            _userRepositoryMock.Setup(repo => repo.DeleteAsync(userId)).Returns(Task.CompletedTask);

            // Act
            await _userService.DeleteUserAsync(userId);

            // Assert
            _userRepositoryMock.Verify(repo => repo.DeleteAsync(userId), Times.Once);
        }
    }
}
