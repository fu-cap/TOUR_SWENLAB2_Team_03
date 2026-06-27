namespace TourPlanner.DataAccessLayer.Entities
{
    public class Waypoint
    {
        public Guid Id { get; set; }
        public Guid TourId { get; set; }
        public int OrderIndex { get; set; }
        public string Label { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}