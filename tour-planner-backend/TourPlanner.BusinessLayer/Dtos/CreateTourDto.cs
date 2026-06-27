using System.ComponentModel.DataAnnotations;
using TourPlanner.DataAccessLayer.Enums;

namespace TourPlanner.BusinessLayer.Dtos
{
    public class WaypointDto
    {
        public string Label { get; set; } = string.Empty;
        [Required] public double Latitude { get; set; }
        [Required] public double Longitude { get; set; }
    }

    public class CreateTourDto
    {
        [Required] public Guid UserId {get; set;}
        [Required] public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        [Required] public List<WaypointDto> Waypoints { get; set; } = new();
        [Required] public TransportType TransportType { get; set; }
    }
}
