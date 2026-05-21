using System.ComponentModel.DataAnnotations;
using TourPlanner.DataAccessLayer.Enums;

namespace TourPlanner.API.Dtos
{
    public class CreateTourDto
    {
        [Required] public int userId {get; set;} = 0;
        [Required] public string name { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        [Required] public string from { get; set; } = string.Empty;
        [Required] public string to { get; set; } = string.Empty;
        [Required] public TransportType TransportType { get; set; }
    }
}