using System.ComponentModel.DataAnnotations;
using TourPlanner.DataAccessLayer.Enums;

namespace TourPlanner.API.Dtos
{
    public class WaypointDto
    {
        public string address { get; set; } = string.Empty;
        [Required] public double latitude { get; set; }
        [Required] public double longitude { get; set; }
    }

    public class CreateTourDto
    {
        [Required] public Guid userId {get; set;}
        [Required] public string name { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        [Required] public List<WaypointDto> waypoints { get; set; } = new();
        [Required] public TransportType TransportType { get; set; }
    }
}