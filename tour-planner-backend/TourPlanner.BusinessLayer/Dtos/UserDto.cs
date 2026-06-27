using System.ComponentModel.DataAnnotations;
using TourPlanner.DataAccessLayer.Enums;

namespace TourPlanner.BusinessLayer.Dtos
{
    public class CreateUserDto
    {
        [Required] public string Username { get; set; } = string.Empty;
        [Required] public string Password { get; set; } = string.Empty;
        [Required] public string Email { get; set; } = string.Empty;
        [Required] public string Gender { get; set; } = string.Empty;
        [Required] public string Firstname { get; set; } = string.Empty;
        [Required] public string Lastname { get; set; } = string.Empty;
    }

    public class UpdateUserDto
    {
        [Required] public string Username { get; set; } = string.Empty;
        [Required(AllowEmptyStrings = true)] public string Password { get; set; } = string.Empty;
        [Required] public string Email { get; set; } = string.Empty;
        [Required] public string Gender { get; set; } = string.Empty;
        [Required] public string Firstname { get; set; } = string.Empty;
        [Required] public string Lastname { get; set; } = string.Empty;
    }

    public class LoginDto
    {
        [Required] public string Username { get; set; } = string.Empty;
        [Required] public string Password { get; set; } = string.Empty;
    }
}
