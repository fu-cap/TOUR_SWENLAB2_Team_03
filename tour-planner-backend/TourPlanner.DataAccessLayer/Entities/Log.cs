using TourPlanner.DataAccessLayer.Enums;

namespace TourPlanner.DataAccessLayer.Entities
{
    public class Log
    {
        public Guid Id { get; set; }
        public Guid tour_id { get; set; }
        public required DateTime date_time { get; set; }
        public required string comment { get; set; }
        public int difficulty { get; set; } = 1;
        public double total_distance_km { get; set; } = 0.0;
        public TimeSpan total_time_min { get; set; } = TimeSpan.Zero;
        public required int rating { get; set; } = 1;
    }
    
}
