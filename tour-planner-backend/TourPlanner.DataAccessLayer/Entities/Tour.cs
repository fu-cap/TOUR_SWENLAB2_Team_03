using TourPlanner.DataAccessLayer.Enums;

namespace TourPlanner.DataAccessLayer.Entities
{
    public class Tour
    {
        public int Id { get; set; }
        public int userID { get; set; }
        public required string Name { get; set; }
        public string Description { get; set; } = "";
        public required double[] From { get; set; }
        public required double[] To { get; set; }
        public TransportType TransportType { get; set; }
        public double Distance_km { get; set; }
        public TimeSpan EstimatedTime { get; set; }
        public string RouteInformation { get; set; } = string.Empty;
        public DateTime CreationDate { get; set; }
        public DateTime LastModifiedDate { get; set; }
    }
}