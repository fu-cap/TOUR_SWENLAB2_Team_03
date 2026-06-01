using TourPlanner.DataAccessLayer.Enums;

namespace TourPlanner.DataAccessLayer.Entities
{
    public class Tour
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public required string Name { get; set; }
        public string Description { get; set; } = "";
        public List<Waypoint> Waypoints { get; set; } = new();
        public TransportType TransportType { get; set; }
        public double DistanceKm { get; set; }
        public TimeSpan EstimatedTime { get; set; }
        public string RouteInformation { get; set; } = string.Empty;
        public double Popularity { get; set; }
        public double ChildFriendliness { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime LastModifiedDate { get; set; }
    }
}