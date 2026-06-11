using System.ComponentModel.DataAnnotations;

namespace TourPlanner.BusinessLayer.Dtos
{
    public class CreateLogDto
    {
        [Required] public Guid TourId { get; set; }
        [Required] public DateTime DateTime { get; set; } = DateTime.Now;
        public string Comment { get; set; } = string.Empty;
        [Required] public int Difficulty { get; set; } = 1;
        public double TotalDistanceKm { get; set; } = 0.0;
        public TimeSpan TotalTimeMin { get; set; } = TimeSpan.Zero;
        public int Rating { get; set; } = 1;
    }

}