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

        // ── Carbon Footprint (computed, not stored in DB) ──────────────────────
        // Baseline: a typical private car emits 120 g CO2/km.
        // Each transport type has its own emission factor:
        //   - Foot / Cycling : 0 g/km        → saves 120 g/km vs. baseline
        //   - DrivingCar     : 120 g/km       → is the baseline; saves 0
        //   - DrivingHgv     : 180 g/km (~50% more) → saves 0
        private const double BaselineCarPerKm = 120.0;

        private double EmittedPerKm => TransportType switch
        {
            TransportType.FootWalking    => 0.0,
            TransportType.FootHiking     => 0.0,
            TransportType.CyclingRegular => 0.0,
            TransportType.CyclingRoad    => 0.0,
            TransportType.DrivingCar     => BaselineCarPerKm,       // car is the reference baseline
            TransportType.DrivingHgv     => BaselineCarPerKm * 1.5, // HGV ~50 % heavier
            _                            => BaselineCarPerKm
        };

        public double Co2EmittedGrams => Math.Round(EmittedPerKm * DistanceKm, 1);

        public double Co2SavedGrams => Math.Round(
            Math.Max(0.0, (BaselineCarPerKm - EmittedPerKm) * DistanceKm), 1);
    }
}