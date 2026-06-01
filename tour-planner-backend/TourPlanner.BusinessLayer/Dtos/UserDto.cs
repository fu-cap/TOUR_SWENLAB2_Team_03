using System.ComponentModel.DataAnnotations;
using TourPlanner.DataAccessLayer.Enums;

namespace TourPlanner.BusinessLayer.Dtos
{
    public class CreateUserDto
    {
        [Required] public string Username { get; set; } = string.Empty;
        [Required] public string Password { get; set; } = string.Empty;
        [Required] public string Email { get; set; } = string.Empty;
    }
}
