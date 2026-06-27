using TourPlanner.DataAccessLayer.Enums;

namespace TourPlanner.DataAccessLayer.Entities
{
    public class Log
    {
        public Guid Id { get; set; }
        public Guid TourId { get; set; }
        public required DateTime DateTime { get; set; }
        public string Comment { get; set; } = string.Empty;
        public int Difficulty { get; set; } = 1;
        public double TotalDistanceKm { get; set; } = 0.0;
        public TimeSpan TotalTimeMin { get; set; } = TimeSpan.Zero;
        public required int Rating { get; set; } = 1;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
    
}
