using System.ComponentModel.DataAnnotations;
using TourPlanner.BusinessLayer.Clients;
using TourPlanner.DataAccessLayer.Enums;

namespace TourPlanner.API.Dtos
{
    public class CreateTourDto
    {
        [Required] public Guid userId {get; set;}
        [Required] public string name { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        [Required] public double[] from { get; set; } = [];
        [Required] public double[] to { get; set; } = [];
        [Required] public TransportType TransportType { get; set; }
    }
}