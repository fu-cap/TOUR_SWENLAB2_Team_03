using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourPlanner.BusinessLayer.Dtos;
using TourPlanner.BusinessLayer.Services;
using TourPlanner.DataAccessLayer.Entities;


namespace TourPlanner.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        private readonly IUserService _userService;

        public UserController(IUserService userService, ILogger<UserController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                _logger.LogInformation("GetAllUsers was called");

                List<User> allUsers = await _userService.GetAllUsersAsync();

                return Ok(allUsers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while exporting users.");
                return StatusCode(500, new {message = "Export failed"});
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            if (!ModelState.IsValid) 
            {
                _logger.LogWarning("Got invalid DTO for creating a user.");
                return BadRequest(ModelState);
            }

            try{
                _logger.LogInformation("Starting creating user: {Username}", dto.Username);

                var createdUser = await _userService.CreateUserAsync(dto);
                
                if(createdUser is not null)
                {
                    return Created();
                }
                throw new Exception("Returned user is null");
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Error in Business Logic: {Username}", dto.Username);
                return BadRequest(new { message = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error while communicating with external API.");
                return StatusCode(503, new { message = "The service is unavailable" });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Username or email already exists: {Username}", dto.Username);
                return StatusCode(409, new { message = "Username or email already exists" });
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while creating the user");
                return StatusCode(500, new { message = "Internal Server Error" });
            }
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetUserById(Guid id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = $"User with ID {id} not found." });
                }
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting user {UserId}", id);
                return StatusCode(500, new { message = "Internal Server Error" });
            }
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserDto dto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Got invalid DTO for updating a user.");
                return BadRequest(ModelState);
            }

            try
            {
                _logger.LogInformation("Starting updating user: {Username}", dto.Username);
                await _userService.UpdateUserAsync(id, dto);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Error in Business Logic: {Username}", dto.Username);
                return BadRequest(new { message = ex.Message });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Username or email already exists: {Username}", dto.Username);
                return StatusCode(409, new { message = "Username or email already exists" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while updating the user");
                return StatusCode(500, new { message = "Internal Server Error" });
            }
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            try
            {
                _logger.LogInformation("Deleting user: {UserId}", id);
                await _userService.DeleteUserAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while deleting the user");
                return StatusCode(500, new { message = "Internal Server Error" });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                _logger.LogInformation("Login attempt for username: {Username}", dto.Username);
                var user = await _userService.AuthenticateAsync(dto.Username, dto.Password);
                if (user == null)
                {
                    return Unauthorized(new { message = "Invalid username or password." });
                }

                return Ok(new { id = user.Id, username = user.Username, email = user.Email });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during login");
                return StatusCode(500, new { message = "Internal Server Error" });
            }
        }
    }
}
