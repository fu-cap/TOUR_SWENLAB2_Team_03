using System;
using System.Collections.Generic;

namespace TourPlanner.BusinessLayer.Dtos
{
    public class WaypointImportDto
    {
        public string Label { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class TourLogImportDto
    {
        public DateTime DateTime { get; set; } = DateTime.UtcNow;
        public string Comment { get; set; } = string.Empty;
        public int Difficulty { get; set; } = 1;
        public double TotalDistanceKm { get; set; }
        public TimeSpan TotalTimeMin { get; set; }
        public int Rating { get; set; } = 1;
    }

    public class TourImportDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string TransportType { get; set; } = string.Empty;
        public double DistanceKm { get; set; }
        public TimeSpan EstimatedTime { get; set; }
        public string RouteInformation { get; set; } = string.Empty;
        public List<WaypointImportDto> Waypoints { get; set; } = new();
        public List<TourLogImportDto> Logs { get; set; } = new();
    }
}
