using Moq;
using Moq.Protected;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using TourPlanner.BusinessLayer.Dtos;
using TourPlanner.BusinessLayer.Services;
using TourPlanner.BusinessLayer.Utils;
using TourPlanner.DataAccessLayer.Entities;
using TourPlanner.DataAccessLayer.Enums;
using TourPlanner.DataAccessLayer.Repositories;

namespace TourPlanner.Tests
{
    [TestFixture]
    public class TourMetricsRobustnessTests
    {
        [Test]
        public void Calculate_NoLogs_ExtremelyLongTour_ShouldClampChildFriendlinessToZero()
        {
            // Arrange
            var tour = new Tour
            {
                Id = Guid.NewGuid(),
                Name = "Extremely Long Tour",
                TransportType = TransportType.FootWalking, // baseScore = 10.0
                DistanceKm = 500.0, // penalty: (500 - 3) * 0.5 = 248.5
                EstimatedTime = TimeSpan.FromHours(10) // 600 min, penalty: (600 - 30) * 0.05 = 28.5
            };
            var logs = new List<Log>();

            // Act
            var (popularity, childFriendliness) = TourMetricsCalculator.Calculate(tour, logs);

            // Assert
            Assert.That(popularity, Is.EqualTo(0));
            // 10.0 - 248.5 - 28.5 = -267.0 -> Clamped to 0.0
            Assert.That(childFriendliness, Is.EqualTo(0.0));
        }

        [Test]
        public void Calculate_WithLogs_ExtremelyLongLogs_ShouldClampChildFriendlinessToZero()
        {
            // Arrange
            var tour = new Tour
            {
                Id = Guid.NewGuid(),
                Name = "Strenuous Hiking Tour",
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
                    Difficulty = 5, // penalty: (5 - 1) * 1.5 = 6.0
                    TotalDistanceKm = 100.0, // penalty: (100 - 3) * 0.5 = 48.5
                    TotalTimeMin = TimeSpan.FromHours(4), // 240 min, penalty: (240 - 30) * 0.05 = 10.5
                    Rating = 2
                }
            };

            // Act
            var (popularity, childFriendliness) = TourMetricsCalculator.Calculate(tour, logs);

            // Assert
            Assert.That(popularity, Is.EqualTo(1));
            // 8.0 - 6.0 - 48.5 - 10.5 = -57.0 -> Clamped to 0.0
            Assert.That(childFriendliness, Is.EqualTo(0.0));
        }

        [Test]
        public void Calculate_NoLogs_IdealChildFriendlyTour_ShouldClampChildFriendlinessToTen()
        {
            // Arrange & Act & Assert for CyclingRegular
            var tourCycling = new Tour
            {
                Id = Guid.NewGuid(),
                Name = "Cycling Tour",
                TransportType = TransportType.CyclingRegular, // baseScore = 10.0
                DistanceKm = 1.5, // <= 3.0 -> penalty 0
                EstimatedTime = TimeSpan.FromMinutes(15) // <= 30 -> penalty 0
            };
            var (_, childFriendlinessCycling) = TourMetricsCalculator.Calculate(tourCycling, new List<Log>());
            Assert.That(childFriendlinessCycling, Is.EqualTo(10.0));

            // Arrange & Act & Assert for FootWalking
            var tourWalking = new Tour
            {
                Id = Guid.NewGuid(),
                Name = "Walking Tour",
                TransportType = TransportType.FootWalking, // baseScore = 10.0
                DistanceKm = 2.9, // <= 3.0 -> penalty 0
                EstimatedTime = TimeSpan.FromMinutes(29) // <= 30 -> penalty 0
            };
            var (_, childFriendlinessWalking) = TourMetricsCalculator.Calculate(tourWalking, new List<Log>());
            Assert.That(childFriendlinessWalking, Is.EqualTo(10.0));
        }

        [Test]
        public void Calculate_WithLogs_IdealChildFriendlyLogs_ShouldClampChildFriendlinessToTen()
        {
            // Arrange
            var tour = new Tour
            {
                Id = Guid.NewGuid(),
                Name = "Cycling Tour with Logs",
                TransportType = TransportType.CyclingRegular, // baseScore = 10.0
                DistanceKm = 10.0,
                EstimatedTime = TimeSpan.FromHours(1)
            };

            var logs = new List<Log>
            {
                new Log
                {
                    Id = Guid.NewGuid(),
                    TourId = tour.Id,
                    DateTime = DateTime.UtcNow,
                    Difficulty = 1, // avgDifficulty = 1 -> penalty: (1 - 1) * 1.5 = 0.0
                    TotalDistanceKm = 2.0, // avgDistance = 2 <= 3 -> penalty 0.0
                    TotalTimeMin = TimeSpan.FromMinutes(20), // avgTime = 20 <= 30 -> penalty 0.0
                    Rating = 5
                }
            };

            // Act
            var (popularity, childFriendliness) = TourMetricsCalculator.Calculate(tour, logs);

            // Assert
            Assert.That(popularity, Is.EqualTo(1));
            // 10.0 - 0.0 - 0.0 - 0.0 = 10.0
            Assert.That(childFriendliness, Is.EqualTo(10.0));
        }

        [Test]
        public async Task UpdateTourAsync_NoLogs_ShouldRecalculateMetricsBasedOnNewTourProperties()
        {
            // Arrange
            var tourRepositoryMock = new Mock<ITourRepository>();
            var logRepositoryMock = new Mock<ILogRepository>();
            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            var httpClient = new HttpClient(httpMessageHandlerMock.Object);

            Environment.SetEnvironmentVariable("OpenRoute_ApiKey", "mock-test-key");

            var tourId = Guid.NewGuid();
            var tour = new Tour
            {
                Id = tourId,
                Name = "Initial Walk",
                TransportType = TransportType.FootWalking, // baseScore = 10.0
                DistanceKm = 2.0,
                EstimatedTime = TimeSpan.FromMinutes(20),
                Popularity = 0.0,
                ChildFriendliness = 10.0
            };

            tourRepositoryMock.Setup(repo => repo.GetByIdAsync(tourId)).ReturnsAsync(tour);
            logRepositoryMock.Setup(repo => repo.GetLogsByTourIdAsync(tourId)).ReturnsAsync(new List<Log>());

            // Mock ORS response for the updated route: DrivingCar, 15km, 40 mins
            var orsResponse = new
            {
                routes = new[]
                {
                    new
                    {
                        summary = new { distance = 15000.0, duration = 2400.0 }, // 15km, 40 mins
                        geometry = "new-geometry"
                    }
                }
            };

            httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = JsonContent.Create(orsResponse)
                });

            Tour? updatedTour = null;
            tourRepositoryMock.Setup(repo => repo.UpdateAsync(It.IsAny<Tour>()))
                .Callback<Tour>(t => updatedTour = t)
                .Returns(Task.CompletedTask);

            var tourService = new TourService(tourRepositoryMock.Object, logRepositoryMock.Object, httpClient);

            var dto = new CreateTourDto
            {
                UserId = Guid.NewGuid(),
                Name = "Updated Driving Tour",
                Description = "Now a car drive",
                TransportType = TransportType.DrivingCar, // baseScore = 3.0
                Waypoints = new List<WaypointDto>
                {
                    new WaypointDto { Label = "Start", Latitude = 48.0, Longitude = 16.0 },
                    new WaypointDto { Label = "End", Latitude = 48.1, Longitude = 16.1 }
                }
            };

            // Act
            await tourService.UpdateTourAsync(tourId, dto);

            // Assert
            Assert.That(updatedTour, Is.Not.Null);
            Assert.That(updatedTour!.Popularity, Is.EqualTo(0.0));
            // Calculated child friendliness on the updated properties (since no logs exist):
            // baseScore = 3.0 (DrivingCar)
            // distance = 15.0km -> penalty: (15 - 3) * 0.5 = 6.0
            // time = 40 mins -> penalty: (40 - 30) * 0.05 = 0.5
            // childFriendliness = 3.0 - 6.0 - 0.5 = -3.5 -> clamped to 0.0
            Assert.That(updatedTour.ChildFriendliness, Is.EqualTo(0.0));
            tourRepositoryMock.Verify(repo => repo.UpdateAsync(tour), Times.Once);
        }

        [Test]
        public async Task UpdateTourAsync_WithLogs_ShouldRecalculateMetricsKeepingLogStatsButUpdatingBaseScore()
        {
            // Arrange
            var tourRepositoryMock = new Mock<ITourRepository>();
            var logRepositoryMock = new Mock<ILogRepository>();
            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            var httpClient = new HttpClient(httpMessageHandlerMock.Object);

            Environment.SetEnvironmentVariable("OpenRoute_ApiKey", "mock-test-key");

            var tourId = Guid.NewGuid();
            var tour = new Tour
            {
                Id = tourId,
                Name = "Hiking Tour",
                TransportType = TransportType.FootHiking, // baseScore = 8.0
                DistanceKm = 5.0,
                EstimatedTime = TimeSpan.FromMinutes(60),
                Popularity = 1.0,
                ChildFriendliness = 6.0
            };

            // Mock 1 log: Difficulty 2, Distance 4km, Time 40 mins
            var log = new Log
            {
                Id = Guid.NewGuid(),
                TourId = tourId,
                Difficulty = 2,
                TotalDistanceKm = 4.0,
                TotalTimeMin = TimeSpan.FromMinutes(40),
                DateTime = DateTime.UtcNow,
                Rating = 4
            };

            tourRepositoryMock.Setup(repo => repo.GetByIdAsync(tourId)).ReturnsAsync(tour);
            logRepositoryMock.Setup(repo => repo.GetLogsByTourIdAsync(tourId)).ReturnsAsync(new List<Log> { log });

            // Mock ORS response: FootWalking, 4km, 40 mins
            var orsResponse = new
            {
                routes = new[]
                {
                    new
                    {
                        summary = new { distance = 4000.0, duration = 2400.0 },
                        geometry = "geometry"
                    }
                }
            };

            httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = JsonContent.Create(orsResponse)
                });

            Tour? updatedTour = null;
            tourRepositoryMock.Setup(repo => repo.UpdateAsync(It.IsAny<Tour>()))
                .Callback<Tour>(t => updatedTour = t)
                .Returns(Task.CompletedTask);

            var tourService = new TourService(tourRepositoryMock.Object, logRepositoryMock.Object, httpClient);

            var dto = new CreateTourDto
            {
                UserId = Guid.NewGuid(),
                Name = "Updated Walking Tour",
                Description = "Walking instead of hiking",
                TransportType = TransportType.FootWalking, // baseScore should change from 8.0 to 10.0!
                Waypoints = new List<WaypointDto>
                {
                    new WaypointDto { Label = "Start", Latitude = 48.0, Longitude = 16.0 },
                    new WaypointDto { Label = "End", Latitude = 48.1, Longitude = 16.1 }
                }
            };

            // Act
            await tourService.UpdateTourAsync(tourId, dto);

            // Assert
            Assert.That(updatedTour, Is.Not.Null);
            Assert.That(updatedTour!.Popularity, Is.EqualTo(1.0));
            // Recalculated child friendliness:
            // baseScore = 10.0 (FootWalking)
            // avgDifficulty = 2.0 -> difficultyPenalty = (2.0 - 1.0) * 1.5 = 1.5
            // avgDistance = 4.0 -> distancePenalty = (4.0 - 3.0) * 0.5 = 0.5
            // avgTime = 40 mins -> timePenalty = (40 - 30) * 0.05 = 0.5
            // childFriendliness = 10.0 - 1.5 - 0.5 - 0.5 = 7.5
            Assert.That(updatedTour.ChildFriendliness, Is.EqualTo(7.5));
            tourRepositoryMock.Verify(repo => repo.UpdateAsync(tour), Times.Once);
        }
    }
}
