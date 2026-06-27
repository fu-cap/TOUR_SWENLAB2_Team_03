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
    public class TourMetricsTests
    {
        #region 1. Pure Unit Tests for TourMetricsCalculator

        [Test]
        public void Calculate_WithNoLogs_FootWalkingTour_ShouldBeHighlyChildFriendly()
        {
            // Arrange
            var tour = new Tour
            {
                Id = Guid.NewGuid(),
                Name = "Short Walking Tour",
                TransportType = TransportType.FootWalking,
                DistanceKm = 2.0, // Short
                EstimatedTime = TimeSpan.FromMinutes(30) // Short
            };
            var logs = new List<Log>();

            // Act
            var (popularity, childFriendliness) = TourMetricsCalculator.Calculate(tour, logs);

            // Assert
            // Popularity should be 0 because there are no logs
            Assert.That(popularity, Is.EqualTo(0));

            // Hand calculation using penalty-based scale:
            // baseScore for FootWalking = 10.0
            // distanceKm = 2.0 <= 3.0 -> penalty = 0.0
            // timeMin = 30.0 <= 30.0 -> penalty = 0.0
            // childFriendliness = 10.0 - 0.0 - 0.0 = 10.0
            Assert.That(childFriendliness, Is.EqualTo(10.0));
        }

        [Test]
        public void Calculate_WithNoLogs_DrivingCarTour_ShouldBeLessChildFriendly()
        {
            // Arrange
            var tour = new Tour
            {
                Id = Guid.NewGuid(),
                Name = "Long Road Trip",
                TransportType = TransportType.DrivingCar,
                DistanceKm = 50.0, // Long
                EstimatedTime = TimeSpan.FromHours(2) // 120 mins
            };
            var logs = new List<Log>();

            // Act
            var (popularity, childFriendliness) = TourMetricsCalculator.Calculate(tour, logs);

            // Assert
            Assert.That(popularity, Is.EqualTo(0));

            // Hand calculation using [0, 10] scale:
            // avgDifficulty (simulated) = 5.0
            // avgDistance (simulated) = 50.0
            // avgTime (simulated) = 120.0
            // difficultyScore = Clamp(10 - (5-1)*2.5, 0, 10) = 0.0
            // distanceScore = Max(0, 10 - 50.0*1.0) = 0.0
            // timeScore = Max(0, 10 - 120 * (10/120)) = 0.0
            // Expected composite = (0 + 0 + 0) / 3 = 0.0
            Assert.That(childFriendliness, Is.EqualTo(0.0));
        }

        [Test]
        public void Calculate_WithLogs_ShouldAverageLogMetrics()
        {
            // Arrange
            var tour = new Tour
            {
                Id = Guid.NewGuid(),
                Name = "Test Tour",
                TransportType = TransportType.CyclingRegular,
                DistanceKm = 10.0,
                EstimatedTime = TimeSpan.FromHours(1)
            };

            // Two logs:
            // Log 1: Difficulty 2, Distance 4km, Time 30 mins
            // Log 2: Difficulty 4, Distance 6km, Time 90 mins
            // Averages: Difficulty = 3.0, Distance = 5.0 km, Time = 60 mins
            var logs = new List<Log>
            {
                new Log
                {
                    Id = Guid.NewGuid(),
                    TourId = tour.Id,
                    DateTime = DateTime.UtcNow,
                    Difficulty = 2,
                    TotalDistanceKm = 4.0,
                    TotalTimeMin = TimeSpan.FromMinutes(30),
                    Rating = 5
                },
                new Log
                {
                    Id = Guid.NewGuid(),
                    TourId = tour.Id,
                    DateTime = DateTime.UtcNow,
                    Difficulty = 4,
                    TotalDistanceKm = 6.0,
                    TotalTimeMin = TimeSpan.FromMinutes(90),
                    Rating = 4
                }
            };

            // Act
            var (popularity, childFriendliness) = TourMetricsCalculator.Calculate(tour, logs);

            // Assert
            // Popularity = logs.Count = 2
            Assert.That(popularity, Is.EqualTo(2));

            // Hand calculation using penalty-based scale:
            // baseScore for CyclingRegular = 10.0
            // avgDifficulty = 3.0 -> difficultyPenalty = (3.0 - 1.0) * 1.5 = 3.0
            // avgDistance = 5.0 -> distancePenalty = (5.0 - 3.0) * 0.5 = 1.0
            // avgTime = 60.0 -> timePenalty = (60.0 - 30.0) * 0.05 = 1.5
            // childFriendliness = 10.0 - 3.0 - 1.0 - 1.5 = 4.5
            Assert.That(childFriendliness, Is.EqualTo(4.5));
        }

        #endregion

        #region 2. Mock-based Service Tests for Hooking Logic

        [Test]
        public async Task CreateTourAsync_ShouldCalculateAndStoreInitialMetrics()
        {
            // Arrange
            var tourRepositoryMock = new Mock<ITourRepository>();
            var logRepositoryMock = new Mock<ILogRepository>();
            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            var httpClient = new HttpClient(httpMessageHandlerMock.Object);

            // Set up environment variable for OpenRoute API key
            Environment.SetEnvironmentVariable("OpenRoute_ApiKey", "mock-test-key");

            // Mock ORS response
            var orsResponse = new
            {
                routes = new[]
                {
                    new
                    {
                        summary = new { distance = 2000.0, duration = 1800.0 }, // 2km, 30 mins
                        geometry = "some-geometry"
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

            Tour? capturedTour = null;
            tourRepositoryMock
                .Setup(repo => repo.AddAsync(It.IsAny<Tour>()))
                .Callback<Tour>(t => capturedTour = t)
                .ReturnsAsync((Tour t) => t);

            // Inject both repositories into TourService
            var tourService = new TourService(tourRepositoryMock.Object, logRepositoryMock.Object, httpClient);

            var dto = new CreateTourDto
            {
                UserId = Guid.NewGuid(),
                Name = "Child-friendly Walking Tour",
                Description = "Short, easy flat walk",
                TransportType = TransportType.FootWalking,
                Waypoints = new List<WaypointDto>
                {
                    new WaypointDto { Label = "StartPoint", Latitude = 48.0, Longitude = 16.0 },
                    new WaypointDto { Label = "EndPoint", Latitude = 48.01, Longitude = 16.01 }
                }
            };

            // Act
            var result = await tourService.CreateTourAsync(dto);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(capturedTour, Is.Not.Null);
            
            // Initial popularity must be 0
            Assert.That(capturedTour!.Popularity, Is.EqualTo(0.0));

            // Initial child-friendliness based on FootWalking, 2km, 30 mins:
            // baseScore = 10.0
            // distancePenalty = 0.0
            // timePenalty = 0.0
            // childFriendliness = 10.0
            Assert.That(capturedTour.ChildFriendliness, Is.EqualTo(10.0));
            
            tourRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Tour>()), Times.Once);
        }

        [Test]
        public async Task CreateLogAsync_ShouldIncrementPopularityAndUpdateChildFriendliness()
        {
            // Arrange
            var tourRepositoryMock = new Mock<ITourRepository>();
            var logRepositoryMock = new Mock<ILogRepository>();
            var logService = new LogService(logRepositoryMock.Object, tourRepositoryMock.Object);

            var tourId = Guid.NewGuid();
            var tour = new Tour
            {
                Id = tourId,
                Name = "Test Tour",
                TransportType = TransportType.CyclingRegular,
                DistanceKm = 10.0,
                EstimatedTime = TimeSpan.FromHours(1),
                Popularity = 0.0,
                ChildFriendliness = 5.0
            };

            tourRepositoryMock.Setup(repo => repo.GetByIdAsync(tourId)).ReturnsAsync(tour);

            var logDto = new CreateLogDto
            {
                TourId = tourId,
                DateTime = DateTime.UtcNow,
                Comment = "Very difficult cycling session",
                Difficulty = 5, // High difficulty
                TotalDistanceKm = 10.0,
                TotalTimeMin = TimeSpan.FromHours(1), // 60 mins
                Rating = 4
            };

            var addedLog = new Log
            {
                Id = Guid.NewGuid(),
                TourId = tourId,
                DateTime = logDto.DateTime,
                Comment = logDto.Comment,
                Difficulty = logDto.Difficulty,
                TotalDistanceKm = logDto.TotalDistanceKm,
                TotalTimeMin = logDto.TotalTimeMin,
                Rating = logDto.Rating
            };

            logRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<Log>())).ReturnsAsync(addedLog);
            logRepositoryMock.Setup(repo => repo.GetLogsByTourIdAsync(tourId)).ReturnsAsync(new List<Log> { addedLog });

            double capturedPopularity = -1;
            double capturedChildFriendliness = -1;
            tourRepositoryMock
                .Setup(repo => repo.UpdateMetricsAsync(tourId, It.IsAny<double>(), It.IsAny<double>()))
                .Callback<Guid, double, double>((_, pop, cf) => { capturedPopularity = pop; capturedChildFriendliness = cf; })
                .Returns(Task.CompletedTask);

            // Act
            var result = await logService.CreateLogAsync(logDto);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(capturedPopularity, Is.Not.EqualTo(-1), "UpdateMetricsAsync should have been called");

            // Popularity should now be 1 (increased by 1)
            Assert.That(capturedPopularity, Is.EqualTo(1.0));

            // Child friendliness calculated with the log:
            // baseScore for CyclingRegular = 10.0
            // difficulty = 5.0 -> difficultyPenalty = (5.0 - 1.0) * 1.5 = 6.0
            // distance = 10.0 -> distancePenalty = (10.0 - 3.0) * 0.5 = 3.5
            // time = 60.0 -> timePenalty = (60.0 - 30.0) * 0.05 = 1.5
            // childFriendliness = 10.0 - 6.0 - 3.5 - 1.5 = -1.0, clamped to 0.0
            Assert.That(capturedChildFriendliness, Is.EqualTo(0.0));

            tourRepositoryMock.Verify(repo => repo.UpdateMetricsAsync(tourId, 1.0, 0.0), Times.Once);
        }

        [Test]
        public async Task UpdateLogAsync_ShouldKeepPopularitySameAndUpdateChildFriendliness()
        {
            // Arrange
            var tourRepositoryMock = new Mock<ITourRepository>();
            var logRepositoryMock = new Mock<ILogRepository>();
            var logService = new LogService(logRepositoryMock.Object, tourRepositoryMock.Object);

            var tourId = Guid.NewGuid();
            var logId = Guid.NewGuid();

            var tour = new Tour
            {
                Id = tourId,
                Name = "Test Tour",
                TransportType = TransportType.CyclingRegular,
                DistanceKm = 10.0,
                EstimatedTime = TimeSpan.FromHours(1),
                Popularity = 1.0,
                ChildFriendliness = 1.67
            };

            var existingLog = new Log
            {
                Id = logId,
                TourId = tourId,
                DateTime = DateTime.UtcNow.AddDays(-1),
                Comment = "Very difficult cycling session",
                Difficulty = 5,
                TotalDistanceKm = 10.0,
                TotalTimeMin = TimeSpan.FromHours(1),
                Rating = 4
            };

            tourRepositoryMock.Setup(repo => repo.GetByIdAsync(tourId)).ReturnsAsync(tour);
            logRepositoryMock.Setup(repo => repo.GetByIdAsync(logId)).ReturnsAsync(existingLog);

            // Updated log details: Much easier log (Difficulty 1, Distance 2km, Time 15 mins)
            var updateDto = new CreateLogDto
            {
                TourId = tourId,
                DateTime = DateTime.UtcNow,
                Comment = "Easy recovery spin",
                Difficulty = 1,
                TotalDistanceKm = 2.0,
                TotalTimeMin = TimeSpan.FromMinutes(15),
                Rating = 5
            };

            logRepositoryMock.Setup(repo => repo.GetLogsByTourIdAsync(tourId))
                .ReturnsAsync(new List<Log> { existingLog }); // Mock returns list containing the modified log entity

            double capturedPopularity = -1;
            double capturedChildFriendliness = -1;
            tourRepositoryMock
                .Setup(repo => repo.UpdateMetricsAsync(tourId, It.IsAny<double>(), It.IsAny<double>()))
                .Callback<Guid, double, double>((_, pop, cf) => { capturedPopularity = pop; capturedChildFriendliness = cf; })
                .Returns(Task.CompletedTask);

            // Act
            await logService.UpdateLogAsync(logId, updateDto);

            // Assert
            Assert.That(capturedPopularity, Is.Not.EqualTo(-1), "UpdateMetricsAsync should have been called");

            // Popularity should still be 1 (unchanged — log count stays the same)
            Assert.That(capturedPopularity, Is.EqualTo(1.0));

            // Child friendliness calculated with updated log details:
            // baseScore = 10.0
            // avgDifficulty = 1.0 -> difficultyPenalty = 0.0
            // avgDistance = 2.0 -> distancePenalty = 0.0
            // avgTime = 15.0 -> timePenalty = 0.0
            // childFriendliness = 10.0
            Assert.That(capturedChildFriendliness, Is.EqualTo(10.0));

            tourRepositoryMock.Verify(repo => repo.UpdateMetricsAsync(tourId, 1.0, 10.0), Times.Once);
        }

        [Test]
        public async Task DeleteLogAsync_ShouldDecrementPopularityAndUpdateChildFriendliness()
        {
            // Arrange
            var tourRepositoryMock = new Mock<ITourRepository>();
            var logRepositoryMock = new Mock<ILogRepository>();
            var logService = new LogService(logRepositoryMock.Object, tourRepositoryMock.Object);

            var tourId = Guid.NewGuid();
            var logId = Guid.NewGuid();

            var tour = new Tour
            {
                Id = tourId,
                Name = "Short Walking Tour",
                TransportType = TransportType.FootWalking,
                DistanceKm = 2.0,
                EstimatedTime = TimeSpan.FromMinutes(30),
                Popularity = 1.0,
                ChildFriendliness = 6.92
            };

            var existingLog = new Log
            {
                Id = logId,
                TourId = tourId,
                DateTime = DateTime.UtcNow,
                Comment = "A log",
                Difficulty = 2,
                TotalDistanceKm = 3.0,
                TotalTimeMin = TimeSpan.FromMinutes(45),
                Rating = 5
            };

            logRepositoryMock.Setup(repo => repo.GetByIdAsync(logId)).ReturnsAsync(existingLog);
            logRepositoryMock.Setup(repo => repo.DeleteAsync(logId)).Returns(Task.CompletedTask);
            tourRepositoryMock.Setup(repo => repo.GetByIdAsync(tourId)).ReturnsAsync(tour);

            // After deletion, no logs remain
            logRepositoryMock.Setup(repo => repo.GetLogsByTourIdAsync(tourId)).ReturnsAsync(new List<Log>());

            double capturedPopularity = -1;
            double capturedChildFriendliness = -1;
            tourRepositoryMock
                .Setup(repo => repo.UpdateMetricsAsync(tourId, It.IsAny<double>(), It.IsAny<double>()))
                .Callback<Guid, double, double>((_, pop, cf) => { capturedPopularity = pop; capturedChildFriendliness = cf; })
                .Returns(Task.CompletedTask);

            // Act
            await logService.DeleteLogAsync(logId);

            // Assert
            Assert.That(capturedPopularity, Is.Not.EqualTo(-1), "UpdateMetricsAsync should have been called");

            // Popularity should now be 0 (no logs left)
            Assert.That(capturedPopularity, Is.EqualTo(0.0));

            // Child friendliness should revert to the simulated values for the unlogged FootWalking tour:
            // baseScore = 10.0, distancePenalty = 0.0, timePenalty = 0.0 → 10.0
            Assert.That(capturedChildFriendliness, Is.EqualTo(10.0));

            logRepositoryMock.Verify(repo => repo.DeleteAsync(logId), Times.Once);
            tourRepositoryMock.Verify(repo => repo.UpdateMetricsAsync(tourId, 0.0, 10.0), Times.Once);
        }

        [Test]
        public void Calculate_WithNoLogs_FootHikingTour_ShouldApplyHikingBaseScoreAndDistancePenalty()
        {
            // Arrange
            var tour = new Tour
            {
                Id = Guid.NewGuid(),
                Name = "Hiking Adventure",
                TransportType = TransportType.FootHiking,
                DistanceKm = 5.0, // distance penalty: (5.0 - 3.0) * 0.5 = 1.0
                EstimatedTime = TimeSpan.FromMinutes(20) // time penalty: 0.0
            };
            var logs = new List<Log>();

            // Act
            var (popularity, childFriendliness) = TourMetricsCalculator.Calculate(tour, logs);

            // Assert
            Assert.That(popularity, Is.EqualTo(0));
            Assert.That(childFriendliness, Is.EqualTo(7.0));
        }

        [Test]
        public void Calculate_WithLogs_ChildFriendlinessClampedToZero_WhenPenaltiesAreExtremelyHigh()
        {
            // Arrange
            var tour = new Tour
            {
                Id = Guid.NewGuid(),
                Name = "Strenuous HGV Drive",
                TransportType = TransportType.DrivingHgv, // baseScore = 1.0
                DistanceKm = 50.0,
                EstimatedTime = TimeSpan.FromHours(2)
            };

            var logs = new List<Log>
            {
                new Log
                {
                    Id = Guid.NewGuid(),
                    TourId = tour.Id,
                    DateTime = DateTime.UtcNow,
                    Difficulty = 5, // penalty = 6.0
                    TotalDistanceKm = 100.0, // penalty = (100 - 3) * 0.5 = 48.5
                    TotalTimeMin = TimeSpan.FromHours(5), // penalty = (300 - 30) * 0.05 = 13.5
                    Rating = 5
                }
            };

            // Act
            var (popularity, childFriendliness) = TourMetricsCalculator.Calculate(tour, logs);

            // Assert
            Assert.That(popularity, Is.EqualTo(1));
            Assert.That(childFriendliness, Is.EqualTo(0.0));
        }

        [Test]
        public async Task UpdateTourAsync_ShouldRecalculateMetricsWhenTourIsUpdated()
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
                Name = "Old Tour Name",
                TransportType = TransportType.CyclingRegular,
                DistanceKm = 10.0,
                EstimatedTime = TimeSpan.FromHours(1),
                Popularity = 0.0,
                ChildFriendliness = 5.0
            };

            tourRepositoryMock.Setup(repo => repo.GetByIdAsync(tourId)).ReturnsAsync(tour);

            // Mock ORS response
            var orsResponse = new
            {
                routes = new[]
                {
                    new
                    {
                        summary = new { distance = 4000.0, duration = 2400.0 }, // 4km, 40 mins
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

            // Mock 1 log for this tour: Difficulty 2, Distance 4km, Time 40 mins (2400 seconds)
            var log = new Log
            {
                Id = Guid.NewGuid(),
                TourId = tourId,
                Difficulty = 2,
                TotalDistanceKm = 4.0,
                TotalTimeMin = TimeSpan.FromMinutes(40),
                DateTime = DateTime.UtcNow,
                Rating = 5
            };
            logRepositoryMock.Setup(repo => repo.GetLogsByTourIdAsync(tourId)).ReturnsAsync(new List<Log> { log });

            Tour? updatedTour = null;
            tourRepositoryMock.Setup(repo => repo.UpdateAsync(It.IsAny<Tour>()))
                .Callback<Tour>(t => updatedTour = t)
                .Returns(Task.CompletedTask);

            var tourService = new TourService(tourRepositoryMock.Object, logRepositoryMock.Object, httpClient);

            var dto = new CreateTourDto
            {
                UserId = Guid.NewGuid(),
                Name = "Updated Tour Name",
                Description = "Updated Description",
                TransportType = TransportType.CyclingRegular,
                Waypoints = new List<WaypointDto>
                {
                    new WaypointDto { Label = "StartPoint", Latitude = 48.0, Longitude = 16.0 },
                    new WaypointDto { Label = "EndPoint", Latitude = 48.01, Longitude = 16.01 }
                }
            };

            // Act
            await tourService.UpdateTourAsync(tourId, dto);

            // Assert
            Assert.That(updatedTour, Is.Not.Null);
            Assert.That(updatedTour!.Popularity, Is.EqualTo(1.0));
            // baseScore = 10.0 (CyclingRegular)
            // avgDifficulty = 2.0 -> difficultyPenalty = (2.0 - 1.0) * 1.5 = 1.5
            // avgDistance = 4.0 -> distancePenalty = (4.0 - 3.0) * 0.5 = 0.5
            // avgTime = 40.0 -> timePenalty = (40.0 - 30.0) * 0.05 = 0.5
            // childFriendliness = 10.0 - 1.5 - 0.5 - 0.5 = 7.5
            Assert.That(updatedTour.ChildFriendliness, Is.EqualTo(7.5));
            tourRepositoryMock.Verify(repo => repo.UpdateAsync(tour), Times.Once);
        }

        [Test]
        public async Task UpdateTourAsync_ShouldRecalculateMetricsBasedOnTourProperties_WhenNoLogsExist()
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
                Name = "Old Tour Name",
                TransportType = TransportType.FootWalking,
                DistanceKm = 2.0,
                EstimatedTime = TimeSpan.FromMinutes(30),
                Popularity = 0.0,
                ChildFriendliness = 10.0
            };

            tourRepositoryMock.Setup(repo => repo.GetByIdAsync(tourId)).ReturnsAsync(tour);

            // Mock ORS response for a much longer route:
            // 20km distance, 120mins (7200 seconds) duration
            var orsResponse = new
            {
                routes = new[]
                {
                    new
                    {
                        summary = new { distance = 20000.0, duration = 7200.0 },
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

            // Mock empty logs list returned for this tour
            logRepositoryMock.Setup(repo => repo.GetLogsByTourIdAsync(tourId)).ReturnsAsync(new List<Log>());

            Tour? updatedTour = null;
            tourRepositoryMock.Setup(repo => repo.UpdateAsync(It.IsAny<Tour>()))
                .Callback<Tour>(t => updatedTour = t)
                .Returns(Task.CompletedTask);

            var tourService = new TourService(tourRepositoryMock.Object, logRepositoryMock.Object, httpClient);

            var dto = new CreateTourDto
            {
                UserId = Guid.NewGuid(),
                Name = "Updated Tour Name",
                Description = "Updated Description",
                TransportType = TransportType.FootHiking, // baseScore = 8.0
                Waypoints = new List<WaypointDto>
                {
                    new WaypointDto { Label = "StartPoint", Latitude = 48.0, Longitude = 16.0 },
                    new WaypointDto { Label = "EndPoint", Latitude = 48.01, Longitude = 16.01 }
                }
            };

            // Act
            await tourService.UpdateTourAsync(tourId, dto);

            // Assert
            Assert.That(updatedTour, Is.Not.Null);
            Assert.That(updatedTour!.Popularity, Is.EqualTo(0.0)); // remains 0 since no logs
            
            // Recalculated child friendliness:
            // baseScore = 8.0 (FootHiking)
            // distanceKm = 20.0 -> distancePenalty = (20.0 - 3.0) * 0.5 = 8.5
            // timeMin = 120.0 -> timePenalty = (120.0 - 30.0) * 0.05 = 4.5
            // childFriendliness = 8.0 - 8.5 - 4.5 = -5.0 -> clamped to 0.0
            Assert.That(updatedTour.ChildFriendliness, Is.EqualTo(0.0));
            tourRepositoryMock.Verify(repo => repo.UpdateAsync(tour), Times.Once);
        }

        #endregion
    }
}
