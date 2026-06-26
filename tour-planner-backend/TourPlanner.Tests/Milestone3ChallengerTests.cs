using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using TourPlanner.BusinessLayer.Dtos;
using TourPlanner.BusinessLayer.Services;
using TourPlanner.DataAccessLayer;
using TourPlanner.DataAccessLayer.Entities;
using TourPlanner.DataAccessLayer.Enums;
using TourPlanner.DataAccessLayer.Repositories;

namespace TourPlanner.Tests
{
    [TestFixture]
    public class Milestone3ChallengerTests
    {
        private TourPlannerDbContext _context = null!;
        private TourRepository _tourRepository = null!;
        private LogRepository _logRepository = null!;
        private Mock<HttpMessageHandler> _httpMessageHandlerMock = null!;
        private HttpClient _httpClient = null!;
        private TourService _tourService = null!;
        private List<Guid> _createdUserIds = null!;

        [SetUp]
        public void SetUp()
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            var options = new DbContextOptionsBuilder<TourPlannerDbContext>()
                .UseNpgsql("Host=localhost;Database=tour_planner;Username=postgres;Password=password")
                .Options;

            _context = new TourPlannerDbContext(options);
            _tourRepository = new TourRepository(_context);
            _logRepository = new LogRepository(_context);
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _tourService = new TourService(_tourRepository, _logRepository, _httpClient, _context);
            _createdUserIds = new List<Guid>();

            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OpenRoute_ApiKey")))
            {
                Environment.SetEnvironmentVariable("OpenRoute_ApiKey", "mock-test-key");
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (_context != null)
            {
                foreach (var userId in _createdUserIds)
                {
                    try
                    {
                        var tours = _context.Tours.Where(t => t.UserId == userId).ToList();
                        foreach (var tour in tours)
                        {
                            var logs = _context.Log.Where(l => l.TourId == tour.Id).ToList();
                            _context.Log.RemoveRange(logs);
                            var waypoints = _context.Waypoints.Where(w => w.TourId == tour.Id).ToList();
                            _context.Waypoints.RemoveRange(waypoints);
                        }
                        _context.Tours.RemoveRange(tours);

                        var user = _context.Users.Find(userId);
                        if (user != null)
                        {
                            _context.Users.Remove(user);
                        }
                        _context.SaveChanges();
                    }
                    catch
                    {
                        // Ignore cleanup issues
                    }
                }
                _context.Dispose();
            }
            _httpClient?.Dispose();
        }

        private async Task<bool> CheckDbConnectionAsync()
        {
            try
            {
                return await _context.Database.CanConnectAsync();
            }
            catch
            {
                return false;
            }
        }

        private async Task<User> CreateTestUserAsync()
        {
            var shortGuid = Guid.NewGuid().ToString().Substring(0, 8);
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = $"challenger_user_{shortGuid}",
                Email = $"challenger_user_{shortGuid}@example.com",
                PasswordHash = "hashed",
                Gender = "Other",
                FirstName = "Challenger",
                LastName = "Test",
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            _createdUserIds.Add(user.Id);
            return user;
        }

        [Test]
        [TestCase(95.0, 16.3, "latitude", "out of range")]
        [TestCase(-90.1, 16.3, "latitude", "out of range")]
        [TestCase(48.2, 185.0, "longitude", "out of range")]
        [TestCase(48.2, -180.1, "longitude", "out of range")]
        public async Task ImportToursAsync_InvalidCoordinateFields_ShouldThrowArgumentException(double lat, double lon, string expectedKeyword1, string expectedKeyword2)
        {
            if (!await CheckDbConnectionAsync())
            {
                Assert.Ignore("Skipping test: Database not accessible.");
                return;
            }

            var user = await CreateTestUserAsync();

            var importDto = new TourImportDto
            {
                Name = "Invalid Coordinates Tour",
                TransportType = "cycling",
                DistanceKm = 10.0,
                EstimatedTime = TimeSpan.FromMinutes(45),
                RouteInformation = "geometry-dummy",
                Waypoints = new List<WaypointImportDto>
                {
                    new WaypointImportDto { Label = "Start", Latitude = 48.0, Longitude = 16.0 },
                    new WaypointImportDto { Label = "Invalid Point", Latitude = lat, Longitude = lon }
                }
            };

            var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
                await _tourService.ImportToursAsync(user.Id, new List<TourImportDto> { importDto }));

            Assert.That(ex.Message, Does.Contain(expectedKeyword1));
            Assert.That(ex.Message, Does.Contain(expectedKeyword2));
        }

        [Test]
        public async Task ImportToursAsync_OfflineRouting_WithValidFallbacks_ShouldSucceed()
        {
            if (!await CheckDbConnectionAsync())
            {
                Assert.Ignore("Skipping test: Database not accessible.");
                return;
            }

            var user = await CreateTestUserAsync();

            // Mock ORS API failure (offline/rate-limited status code)
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.TooManyRequests,
                    Content = new StringContent("Rate limit exceeded")
                });

            var importDto = new TourImportDto
            {
                Name = "Offline Routing Success",
                TransportType = "cycling",
                DistanceKm = 15.75,
                EstimatedTime = TimeSpan.FromMinutes(50),
                RouteInformation = "fallback-geometry-string",
                Waypoints = new List<WaypointImportDto>
                {
                    new WaypointImportDto { Label = "A", Latitude = 48.2, Longitude = 16.3 },
                    new WaypointImportDto { Label = "B", Latitude = 48.3, Longitude = 16.4 }
                }
            };

            // Import - should succeed by catching the exception and falling back to the DTO values
            await _tourService.ImportToursAsync(user.Id, new List<TourImportDto> { importDto });

            // Verify imported tour contains the fallback values
            var tours = await _tourRepository.GetToursByUserIdAsync(user.Id);
            Assert.That(tours.Count, Is.EqualTo(1));
            Assert.That(tours[0].Name, Is.EqualTo("Offline Routing Success"));
            Assert.That(tours[0].DistanceKm, Is.EqualTo(15.75));
            Assert.That(tours[0].EstimatedTime, Is.EqualTo(TimeSpan.FromMinutes(50)));
            Assert.That(tours[0].RouteInformation, Is.EqualTo("fallback-geometry-string"));
        }

        [Test]
        public async Task ImportToursAsync_OfflineRouting_WithoutValidFallbacks_ShouldThrowArgumentException()
        {
            if (!await CheckDbConnectionAsync())
            {
                Assert.Ignore("Skipping test: Database not accessible.");
                return;
            }

            var user = await CreateTestUserAsync();

            // Mock ORS API failure (offline)
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.ServiceUnavailable,
                    Content = new StringContent("Service offline")
                });

            var importDto = new TourImportDto
            {
                Name = "Offline Routing Failure",
                TransportType = "cycling",
                DistanceKm = 0, // No valid fallback distance
                EstimatedTime = TimeSpan.Zero, // No valid fallback time
                RouteInformation = "",
                Waypoints = new List<WaypointImportDto>
                {
                    new WaypointImportDto { Label = "A", Latitude = 48.2, Longitude = 16.3 },
                    new WaypointImportDto { Label = "B", Latitude = 48.3, Longitude = 16.4 }
                }
            };

            // Import - should fail because ORS is offline, and no valid fallback values are provided
            var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
                await _tourService.ImportToursAsync(user.Id, new List<TourImportDto> { importDto }));

            Assert.That(ex.Message, Does.Contain("distance").Or.Contain("time"));
        }

        [Test]
        public async Task ImportToursAsync_UserIsolation_ShouldEnsureStrictAccessControl()
        {
            if (!await CheckDbConnectionAsync())
            {
                Assert.Ignore("Skipping test: Database not accessible.");
                return;
            }

            var userA = await CreateTestUserAsync();
            var userB = await CreateTestUserAsync();

            var importDto = new TourImportDto
            {
                Name = "User A Tour Only",
                TransportType = "cycling",
                DistanceKm = 5.0,
                EstimatedTime = TimeSpan.FromMinutes(20),
                RouteInformation = "geom-a",
                Waypoints = new List<WaypointImportDto>
                {
                    new WaypointImportDto { Latitude = 48.0, Longitude = 16.0 },
                    new WaypointImportDto { Latitude = 48.1, Longitude = 16.1 }
                }
            };

            // Import for User A
            await _tourService.ImportToursAsync(userA.Id, new List<TourImportDto> { importDto });

            // Verify User A can load it
            var toursForA = await _tourRepository.GetToursByUserIdAsync(userA.Id);
            Assert.That(toursForA.Count, Is.EqualTo(1));
            Assert.That(toursForA[0].Name, Is.EqualTo("User A Tour Only"));

            // Verify User B cannot load it (GetToursByUserIdAsync returns 0 tours)
            var toursForB = await _tourRepository.GetToursByUserIdAsync(userB.Id);
            Assert.That(toursForB.Count, Is.EqualTo(0));

            // Verify ExportToursAsync for User B returns 0 tours
            var exportedForB = await _tourService.ExportToursAsync(userB.Id);
            Assert.That(exportedForB.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task ImportToursAsync_TransactionRollback_WhenMixedValidityArrayImported()
        {
            if (!await CheckDbConnectionAsync())
            {
                Assert.Ignore("Skipping test: Database not accessible.");
                return;
            }

            var user = await CreateTestUserAsync();

            // First tour is completely valid
            var validTour = new TourImportDto
            {
                Name = "Valid Tour in Batch",
                TransportType = "hiking",
                DistanceKm = 8.5,
                EstimatedTime = TimeSpan.FromHours(2),
                RouteInformation = "geom-valid",
                Waypoints = new List<WaypointImportDto>
                {
                    new WaypointImportDto { Label = "Start", Latitude = 48.0, Longitude = 16.0 },
                    new WaypointImportDto { Label = "End", Latitude = 48.1, Longitude = 16.1 }
                },
                Logs = new List<TourLogImportDto>
                {
                    new TourLogImportDto
                    {
                        Comment = "Valid Log",
                        Difficulty = 3,
                        Rating = 4,
                        TotalDistanceKm = 8.5,
                        TotalTimeMin = TimeSpan.FromHours(2)
                    }
                }
            };

            // Second tour is invalid (invalid log difficulty = 99)
            var invalidTour = new TourImportDto
            {
                Name = "Invalid Tour in Batch",
                TransportType = "hiking",
                DistanceKm = 10.0,
                EstimatedTime = TimeSpan.FromHours(2.5),
                RouteInformation = "geom-invalid",
                Waypoints = new List<WaypointImportDto>
                {
                    new WaypointImportDto { Label = "Start", Latitude = 48.0, Longitude = 16.0 },
                    new WaypointImportDto { Label = "End", Latitude = 48.1, Longitude = 16.1 }
                },
                Logs = new List<TourLogImportDto>
                {
                    new TourLogImportDto
                    {
                        Comment = "Invalid Log Difficulty",
                        Difficulty = 99, // Out of range [1..5]
                        Rating = 3,
                        TotalDistanceKm = 10.0,
                        TotalTimeMin = TimeSpan.FromHours(2.5)
                    }
                }
            };

            var batch = new List<TourImportDto> { validTour, invalidTour };

            // Import - should throw an ArgumentException due to the invalid log difficulty
            var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
                await _tourService.ImportToursAsync(user.Id, batch));

            Assert.That(ex.Message, Does.Contain("difficulty").And.Contain("between 1 and 5"));

            // Verify that transaction rolled back: NO tours were created for the user
            var tours = await _tourRepository.GetToursByUserIdAsync(user.Id);
            Assert.That(tours.Count, Is.EqualTo(0), "No tours should have been saved in the DB due to transaction rollback.");

            // Verify no logs exist in DB for the user's tours (since there are no tours, this should naturally be 0)
            var allToursInDb = await _context.Tours.Where(t => t.UserId == user.Id).ToListAsync();
            Assert.That(allToursInDb.Count, Is.EqualTo(0));
            
            var allLogsInDb = await _context.Log.Where(l => allToursInDb.Select(t => t.Id).Contains(l.TourId)).ToListAsync();
            Assert.That(allLogsInDb.Count, Is.EqualTo(0));
        }
    }
}
