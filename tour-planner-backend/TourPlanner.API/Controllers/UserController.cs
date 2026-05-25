using Microsoft.AspNetCore.Mvc;
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
    }
}