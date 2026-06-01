using TourPlanner.DataAccessLayer.Enums;

namespace TourPlanner.DataAccessLayer.Entities
{
    public class Log
    {
        public Guid Id { get; set; }
        public Guid TourId { get; set; }
        public required DateTime DateTime { get; set; }
        public required string Comment { get; set; }
        public int Difficulty { get; set; } = 1;
        public double TotalDistanceKm { get; set; } = 0.0;
        public TimeSpan TotalTimeMin { get; set; } = TimeSpan.Zero;
        public required int Rating { get; set; } = 1;
    }
    
}
