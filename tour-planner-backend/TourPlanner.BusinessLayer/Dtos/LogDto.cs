using System.ComponentModel.DataAnnotations;

namespace TourPlanner.BusinessLayer.Dtos
{
    public class CreateLogDto
    {
        [Required] public Guid tour_id { get; set; }
        [Required] public DateTime date_time { get; set; } = DateTime.Now;
        [Required] public string comment { get; set; } = string.Empty;
        [Required] public int difficulty { get; set; } = 1;
        public double total_distance_km { get; set; } = 0.0;
        public TimeSpan total_time_min { get; set; } = TimeSpan.Zero;
        public int rating { get; set; } = 1; 
    }
}