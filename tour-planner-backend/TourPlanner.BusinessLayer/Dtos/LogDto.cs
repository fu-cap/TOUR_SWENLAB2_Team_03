using System.ComponentModel.DataAnnotations;

namespace TourPlanner.BusinessLayer.Dtos
{
    public class CreateLogDto
    {
        [Required] public Guid TourId { get; set; }
        [Required] public DateTime DateTime { get; set; } = DateTime.Now;
        public string Comment { get; set; } = string.Empty;
        
        [Required]
        [Range(1, 5, ErrorMessage = "Difficulty must be between 1 and 5.")]
        public int Difficulty { get; set; } = 1;
        
        [Range(0, double.MaxValue, ErrorMessage = "Total distance cannot be negative.")]
        public double TotalDistanceKm { get; set; } = 0.0;
        public TimeSpan TotalTimeMin { get; set; } = TimeSpan.Zero;
        
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; } = 1;
    }

}