using NUnit.Framework;
using TourPlanner.BusinessLayer.Utils;
using TourPlanner.DataAccessLayer.Entities;
using TourPlanner.DataAccessLayer.Enums;

namespace TourPlanner.Tests
{
    /// <summary>
    /// Unit tests for the Carbon Footprint Estimator (Milestone 4).
    /// Verifies CO2 computation logic in TourMetricsCalculator and the Tour entity's
    /// computed Co2SavedGrams / Co2EmittedGrams properties.
    /// </summary>
    [TestFixture]
    public class CarbonFootprintTests
    {
        // ─────────────────────────────────────────────────────────────────────
        // Helper: build a minimal Tour with given transport type and distance
        // ─────────────────────────────────────────────────────────────────────
        private static Tour MakeTour(TransportType transport, double distanceKm) => new Tour
        {
            Id = Guid.NewGuid(),
            Name = "Test Tour",
            TransportType = transport,
            DistanceKm = distanceKm,
            UserId = Guid.NewGuid(),
            EstimatedTime = TimeSpan.FromHours(1),
        };

        // ─────────────────────────────────────────────────────────────────────
        // 1. Zero-emission transport types save exactly 120g/km
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void FootWalking_10km_SavesMaxCo2_EmitsZero()
        {
            var tour = MakeTour(TransportType.FootWalking, 10.0);

            Assert.That(tour.Co2EmittedGrams, Is.EqualTo(0.0));
            Assert.That(tour.Co2SavedGrams, Is.EqualTo(1200.0)); // 120g/km * 10km
        }

        [Test]
        public void FootHiking_5km_SavesMaxCo2_EmitsZero()
        {
            var tour = MakeTour(TransportType.FootHiking, 5.0);

            Assert.That(tour.Co2EmittedGrams, Is.EqualTo(0.0));
            Assert.That(tour.Co2SavedGrams, Is.EqualTo(600.0)); // 120g * 5km
        }

        [Test]
        public void CyclingRegular_20km_EmitsZero_SavesFullBaseline()
        {
            var tour = MakeTour(TransportType.CyclingRegular, 20.0);

            Assert.That(tour.Co2EmittedGrams, Is.EqualTo(0.0));
            Assert.That(tour.Co2SavedGrams, Is.EqualTo(2400.0));
        }

        [Test]
        public void CyclingRoad_15km_EmitsZero_SavesFullBaseline()
        {
            var tour = MakeTour(TransportType.CyclingRoad, 15.0);

            Assert.That(tour.Co2EmittedGrams, Is.EqualTo(0.0));
            Assert.That(tour.Co2SavedGrams, Is.EqualTo(1800.0));
        }

        // ─────────────────────────────────────────────────────────────────────
        // 2. DrivingCar: emits exactly 120g/km, saves 0
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void DrivingCar_10km_EmitsBaseline_SavesZero()
        {
            var tour = MakeTour(TransportType.DrivingCar, 10.0);

            Assert.That(tour.Co2EmittedGrams, Is.EqualTo(1200.0));
            Assert.That(tour.Co2SavedGrams, Is.EqualTo(0.0));
        }

        [Test]
        public void DrivingCar_50km_EmitsCorrect()
        {
            var tour = MakeTour(TransportType.DrivingCar, 50.0);

            Assert.That(tour.Co2EmittedGrams, Is.EqualTo(6000.0));
            Assert.That(tour.Co2SavedGrams, Is.EqualTo(0.0));
        }

        // ─────────────────────────────────────────────────────────────────────
        // 3. DrivingHgv: emits 180g/km (120 * 1.5), saves 0
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void DrivingHgv_10km_EmitsMoreThanCar_SavesZero()
        {
            var tour = MakeTour(TransportType.DrivingHgv, 10.0);

            Assert.That(tour.Co2EmittedGrams, Is.EqualTo(1800.0)); // 180g * 10
            Assert.That(tour.Co2SavedGrams, Is.EqualTo(0.0));
        }

        // ─────────────────────────────────────────────────────────────────────
        // 4. Edge case: zero distance
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void ZeroDistance_AllTransportTypes_BothValuesZero()
        {
            foreach (var transport in Enum.GetValues<TransportType>())
            {
                var tour = MakeTour(transport, 0.0);
                Assert.That(tour.Co2EmittedGrams, Is.EqualTo(0.0),
                    $"Expected 0 emitted for {transport} at 0km");
                Assert.That(tour.Co2SavedGrams, Is.EqualTo(0.0),
                    $"Expected 0 saved for {transport} at 0km");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // 5. Rounding: result is rounded to 1 decimal place
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void Rounding_FractionalDistance_RoundedToOneDecimal()
        {
            var tour = MakeTour(TransportType.FootWalking, 3.333);
            // 120 * 3.333 = 399.96 -> rounded to 400.0
            Assert.That(tour.Co2SavedGrams, Is.EqualTo(Math.Round(120.0 * 3.333, 1)));
        }

        // ─────────────────────────────────────────────────────────────────────
        // 6. TourMetricsCalculator.ComputeCo2 static method
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void ComputeCo2_FootWalking_10km_MatchesEntityValues()
        {
            var (saved, emitted) = TourMetricsCalculator.ComputeCo2(TransportType.FootWalking, 10.0);
            var tour = MakeTour(TransportType.FootWalking, 10.0);

            Assert.That(saved,   Is.EqualTo(tour.Co2SavedGrams));
            Assert.That(emitted, Is.EqualTo(tour.Co2EmittedGrams));
        }

        [Test]
        public void ComputeCo2_DrivingCar_25km_MatchesEntityValues()
        {
            var (saved, emitted) = TourMetricsCalculator.ComputeCo2(TransportType.DrivingCar, 25.0);
            var tour = MakeTour(TransportType.DrivingCar, 25.0);

            Assert.That(saved,   Is.EqualTo(tour.Co2SavedGrams));
            Assert.That(emitted, Is.EqualTo(tour.Co2EmittedGrams));
        }

        // ─────────────────────────────────────────────────────────────────────
        // 7. Invariant: saved + emitted always >= carBaseline for non-HGV
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void Invariant_SavedPlusEmitted_EqualsOrExceedsCarBaseline_ForNonHgv()
        {
            double distanceKm = 10.0;
            double baseline = 120.0 * distanceKm;

            foreach (var transport in Enum.GetValues<TransportType>())
            {
                if (transport == TransportType.DrivingHgv) continue; // HGV exceeds baseline intentionally

                var tour = MakeTour(transport, distanceKm);
                var total = tour.Co2SavedGrams + tour.Co2EmittedGrams;

                Assert.That(total, Is.EqualTo(baseline).Within(0.01),
                    $"saved + emitted should equal car baseline for {transport}");
            }
        }
    }
}
