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
                _logger.LogInformation("Starting creating user: {Username}", dto.username);

                var createdUser = await _userService.CreateUserAsync(dto);
                
                if(createdUser is not null)
                {
                    return Created(string.Empty, createdUser);
                }

                _logger.LogWarning("User creation failed without throwing an exception: {Username}", dto.username);
                return BadRequest(new { message = "User could not be created." });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Error in Business Logic: {Username}", dto.username);
                return BadRequest(new { message = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error while communicating with external API.");
                return StatusCode(503, new { message = "The service is unavailable" });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Username or email already exists: {Username}", dto.username);
                return StatusCode(409, new { message = "Username or email already exists" });
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while creating the user");
                return StatusCode(500, new { message = "Internal Server Error" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(Guid id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { message = $"User with ID {id} not found." });
            }
            return Ok(user);
        }
    }
}