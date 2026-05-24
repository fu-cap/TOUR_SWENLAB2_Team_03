using System.ComponentModel.DataAnnotations;
using TourPlanner.DataAccessLayer.Enums;

namespace TourPlanner.BusinessLayer.Dtos
{
    public class CreateUserDto
    {
        [Required] public string username { get; set; } = string.Empty;
        [Required] public string password_hash { get; set; } = string.Empty;
        [Required] public string email { get; set; } = string.Empty;
    }
}