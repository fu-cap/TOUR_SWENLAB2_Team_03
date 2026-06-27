using NUnit.Framework;
using System;
using System.Collections.Generic;
using TourPlanner.BusinessLayer.Utils;
using TourPlanner.DataAccessLayer.Entities;
using TourPlanner.DataAccessLayer.Enums;

namespace TourPlanner.Tests
{
    [TestFixture]
    public class TourMetricsExtremeTests
    {
        [Test]
        public void Calculate_NoLogs_ZeroDistanceAndTime_ShouldClampCorrectly()
        {
            var tour = new Tour
            {
                Id = Guid.NewGuid(),
                Name = "Zero Tour",
                TransportType = TransportType.FootWalking, // baseScore = 10.0
                DistanceKm = 0.0,
                EstimatedTime = TimeSpan.Zero
            };
            var logs = new List<Log>();

            var (popularity, childFriendliness) = TourMetricsCalculator.Calculate(tour, logs);

            Assert.That(popularity, Is.EqualTo(0));
            Assert.That(childFriendliness, Is.EqualTo(10.0));
        }

        [Test]
        public void Calculate_NoLogs_HugeDistanceAndTime_ShouldClampToZero()
        {
            var tour = new Tour
            {
                Id = Guid.NewGuid(),
                Name = "Huge Tour",
                TransportType = TransportType.FootWalking, // baseScore = 10.0
                DistanceKm = 1000.0, // penalty: (1000 - 3) * 0.5 = 498.5
                EstimatedTime = TimeSpan.FromHours(1000) // penalty: (60000 - 30) * 0.05 = 2998.5
            };
            var logs = new List<Log>();

            var (popularity, childFriendliness) = TourMetricsCalculator.Calculate(tour, logs);

            Assert.That(popularity, Is.EqualTo(0));
            // Penalty of ~3497 should result in 0.0 due to clamping
            Assert.That(childFriendliness, Is.EqualTo(0.0));
        }

        [Test]
        public void Calculate_WithLogs_DifficultyZero_ShouldHandleNegativePenaltyAndClampToTen()
        {
            var tour = new Tour
            {
                Id = Guid.NewGuid(),
                Name = "Foot Hiking Tour",
                TransportType = TransportType.FootHiking, // baseScore = 8.0
                DistanceKm = 2.0,
                EstimatedTime = TimeSpan.FromMinutes(20)
            };

            var logs = new List<Log>
            {
                new Log
                {
                    Id = Guid.NewGuid(),
                    TourId = tour.Id,
                    DateTime = DateTime.UtcNow,
                    Difficulty = 0, // avgDifficulty = 0.0 -> penalty = (0 - 1) * 1.5 = -1.5
                    TotalDistanceKm = 2.0,
                    TotalTimeMin = TimeSpan.FromMinutes(20),
                    Rating = 5
                }
            };

            var (popularity, childFriendliness) = TourMetricsCalculator.Calculate(tour, logs);

            Assert.That(popularity, Is.EqualTo(1));
            // 8.0 - (-1.5) - 0.0 - 0.0 = 9.5
            Assert.That(childFriendliness, Is.EqualTo(9.5));
        }

        [Test]
        public void Calculate_WithLogs_DifficultyZero_FootWalking_ShouldClampToTen()
        {
            var tour = new Tour
            {
                Id = Guid.NewGuid(),
                Name = "Foot Walking Tour",
                TransportType = TransportType.FootWalking, // baseScore = 10.0
                DistanceKm = 2.0,
                EstimatedTime = TimeSpan.FromMinutes(20)
            };

            var logs = new List<Log>
            {
                new Log
                {
                    Id = Guid.NewGuid(),
                    TourId = tour.Id,
                    DateTime = DateTime.UtcNow,
                    Difficulty = 0, // penalty = -1.5
                    TotalDistanceKm = 2.0,
                    TotalTimeMin = TimeSpan.FromMinutes(20),
                    Rating = 5
                }
            };

            var (popularity, childFriendliness) = TourMetricsCalculator.Calculate(tour, logs);

            Assert.That(popularity, Is.EqualTo(1));
            // 10.0 - (-1.5) = 11.5 -> Clamped to 10.0
            Assert.That(childFriendliness, Is.EqualTo(10.0));
        }

        [Test]
        public void Calculate_WithLogs_DifficultySix_ShouldApplyHighPenalty_ResultIs0_5()
        {
            var tour = new Tour
            {
                Id = Guid.NewGuid(),
                Name = "Foot Hiking Tour",
                TransportType = TransportType.FootHiking, // baseScore = 8.0
                DistanceKm = 2.0,
                EstimatedTime = TimeSpan.FromMinutes(20)
            };

            var logs = new List<Log>
            {
                new Log
                {
                    Id = Guid.NewGuid(),
                    TourId = tour.Id,
                    DateTime = DateTime.UtcNow,
                    Difficulty = 6, // avgDifficulty = 6.0 -> penalty = (6 - 1) * 1.5 = 7.5
                    TotalDistanceKm = 2.0,
                    TotalTimeMin = TimeSpan.FromMinutes(20),
                    Rating = 5
                }
            };

            var (popularity, childFriendliness) = TourMetricsCalculator.Calculate(tour, logs);

            Assert.That(popularity, Is.EqualTo(1));
            // 8.0 - 7.5 - 0.0 - 0.0 = 0.5
            Assert.That(childFriendliness, Is.EqualTo(0.5));
        }

        [Test]
        public void Calculate_WithLogs_HugeLogsDistanceAndTime_ShouldClampToZero()
        {
            var tour = new Tour
            {
                Id = Guid.NewGuid(),
                Name = "Foot Hiking Tour",
                TransportType = TransportType.FootHiking,
                DistanceKm = 2.0,
                EstimatedTime = TimeSpan.FromMinutes(20)
            };

            var logs = new List<Log>
            {
                new Log
                {
                    Id = Guid.NewGuid(),
                    TourId = tour.Id,
                    DateTime = DateTime.UtcNow,
                    Difficulty = 3, // penalty: (3 - 1) * 1.5 = 3.0
                    TotalDistanceKm = 1000.0, // penalty: (1000 - 3) * 0.5 = 498.5
                    TotalTimeMin = TimeSpan.FromHours(1000), // penalty: (60000 - 30) * 0.05 = 2998.5
                    Rating = 5
                }
            };

            var (popularity, childFriendliness) = TourMetricsCalculator.Calculate(tour, logs);

            Assert.That(popularity, Is.EqualTo(1));
            // Score will be negative and clamped to 0.0
            Assert.That(childFriendliness, Is.EqualTo(0.0));
        }
    }
}
