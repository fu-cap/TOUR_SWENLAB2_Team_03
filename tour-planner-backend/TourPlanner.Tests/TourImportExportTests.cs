using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
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
    public class TourImportExportTests
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
                Username = $"user_{shortGuid}",
                Email = $"user_{shortGuid}@example.com",
                PasswordHash = "hashed",
                Gender = "Other",
                FirstName = "Test",
                LastName = "User",
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            _createdUserIds.Add(user.Id);
            return user;
        }

        [Test]
        public async Task ExportToursAsync_ShouldReturnCorrectData()
        {
            if (!await CheckDbConnectionAsync())
            {
                Assert.Ignore("Skipping test: Database not accessible.");
                return;
            }

            var user = await CreateTestUserAsync();
            
            // Add a tour to export
            var tour = new Tour
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Name = "Vienna Sightseeing",
                Description = "Lovely tour around Vienna",
                TransportType = TransportType.CyclingRegular,
                DistanceKm = 5.5,
                EstimatedTime = TimeSpan.FromMinutes(25),
                RouteInformation = "geometry-xyz",
                CreationDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow,
                Waypoints = new List<Waypoint>
                {
                    new Waypoint { Label = "Start", Latitude = 48.2, Longitude = 16.3, OrderIndex = 0 },
                    new Waypoint { Label = "End", Latitude = 48.21, Longitude = 16.31, OrderIndex = 1 }
                }
            };

            await _tourRepository.AddAsync(tour);

            // Add log
            var log = new Log
            {
                Id = Guid.NewGuid(),
                TourId = tour.Id,
                DateTime = DateTime.UtcNow,
                Comment = "Great trip!",
                Difficulty = 2,
                TotalDistanceKm = 5.5,
                TotalTimeMin = TimeSpan.FromMinutes(25),
                Rating = 5,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Log.Add(log);
            await _context.SaveChangesAsync();

            // Export
            var exported = await _tourService.ExportToursAsync(user.Id);

            // Assert
            Assert.That(exported, Is.Not.Null);
            Assert.That(exported.Count, Is.EqualTo(1));
            Assert.That(exported[0].Name, Is.EqualTo("Vienna Sightseeing"));
            Assert.That(exported[0].TransportType, Is.EqualTo("cycling-regular"));
            Assert.That(exported[0].Waypoints.Count, Is.EqualTo(2));
            Assert.That(exported[0].Logs.Count, Is.EqualTo(1));
            Assert.That(exported[0].Logs[0].Comment, Is.EqualTo("Great trip!"));
        }

        [Test]
        public async Task ImportToursAsync_Success_ShouldInsertToursAndLogs()
        {
            if (!await CheckDbConnectionAsync())
            {
                Assert.Ignore("Skipping test: Database not accessible.");
                return;
            }

            var user = await CreateTestUserAsync();

            var importDto = new TourImportDto
            {
                Name = "Imported Tour",
                Description = "Description",
                TransportType = "cycling",
                DistanceKm = 10.0,
                EstimatedTime = TimeSpan.FromMinutes(45),
                RouteInformation = "some-geometry",
                Waypoints = new List<WaypointImportDto>
                {
                    new WaypointImportDto { Label = "A", Latitude = 48.2, Longitude = 16.3 },
                    new WaypointImportDto { Label = "B", Latitude = 48.3, Longitude = 16.4 }
                },
                Logs = new List<TourLogImportDto>
                {
                    new TourLogImportDto
                    {
                        Comment = "Imported log",
                        Difficulty = 2,
                        Rating = 4,
                        TotalDistanceKm = 10.0,
                        TotalTimeMin = TimeSpan.FromMinutes(45)
                    }
                }
            };

            // Import
            await _tourService.ImportToursAsync(user.Id, new List<TourImportDto> { importDto });

            // Check DB
            var tours = await _tourRepository.GetToursByUserIdAsync(user.Id);
            Assert.That(tours.Count, Is.EqualTo(1));
            Assert.That(tours[0].Name, Is.EqualTo("Imported Tour"));
            Assert.That(tours[0].TransportType, Is.EqualTo(TransportType.CyclingRegular));
            Assert.That(tours[0].Popularity, Is.EqualTo(1.0)); // 1 log imported
            
            var logs = await _logRepository.GetLogsByTourIdAsync(tours[0].Id);
            Assert.That(logs.Count, Is.EqualTo(1));
            Assert.That(logs[0].Comment, Is.EqualTo("Imported log"));
        }

        [Test]
        public async Task ImportToursAsync_FallbackRouting_ShouldUseProvidedIfOrsFails()
        {
            if (!await CheckDbConnectionAsync())
            {
                Assert.Ignore("Skipping test: Database not accessible.");
                return;
            }

            var user = await CreateTestUserAsync();

            // Mock ORS API failure
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Content = new StringContent("ORS error")
                });

            var importDto = new TourImportDto
            {
                Name = "Fallback Route Tour",
                Description = "Desc",
                TransportType = "hiking",
                DistanceKm = 12.34,
                EstimatedTime = TimeSpan.FromHours(3),
                RouteInformation = "geometry-fallback",
                Waypoints = new List<WaypointImportDto>
                {
                    new WaypointImportDto { Label = "Start", Latitude = 48.0, Longitude = 16.0 },
                    new WaypointImportDto { Label = "End", Latitude = 48.1, Longitude = 16.1 }
                }
            };

            // Import
            await _tourService.ImportToursAsync(user.Id, new List<TourImportDto> { importDto });

            // Check DB
            var tours = await _tourRepository.GetToursByUserIdAsync(user.Id);
            Assert.That(tours.Count, Is.EqualTo(1));
            Assert.That(tours[0].DistanceKm, Is.EqualTo(12.34));
            Assert.That(tours[0].EstimatedTime, Is.EqualTo(TimeSpan.FromHours(3)));
            Assert.That(tours[0].RouteInformation, Is.EqualTo("geometry-fallback"));
        }

        [Test]
        public async Task ImportToursAsync_TransportTypeParsing_ShouldMapCorrectly()
        {
            if (!await CheckDbConnectionAsync())
            {
                Assert.Ignore("Skipping test: Database not accessible.");
                return;
            }

            var user = await CreateTestUserAsync();

            var importDto = new TourImportDto
            {
                Name = "Transport Parse Tour",
                Description = "Desc",
                TransportType = "Hike", // Should parse to FootHiking
                DistanceKm = 4.0,
                EstimatedTime = TimeSpan.FromHours(1),
                RouteInformation = "geom",
                Waypoints = new List<WaypointImportDto>
                {
                    new WaypointImportDto { Label = "A", Latitude = 48.0, Longitude = 16.0 },
                    new WaypointImportDto { Label = "B", Latitude = 48.1, Longitude = 16.1 }
                }
            };

            // Import
            await _tourService.ImportToursAsync(user.Id, new List<TourImportDto> { importDto });

            // Check DB
            var tours = await _tourRepository.GetToursByUserIdAsync(user.Id);
            Assert.That(tours[0].TransportType, Is.EqualTo(TransportType.FootHiking));
        }

        [Test]
        public async Task ImportToursAsync_InvalidInputs_ShouldThrowArgumentException()
        {
            if (!await CheckDbConnectionAsync())
            {
                Assert.Ignore("Skipping test: Database not accessible.");
                return;
            }

            var user = await CreateTestUserAsync();

            // 1. Empty Name
            var emptyNameDto = new TourImportDto
            {
                Name = "",
                TransportType = "cycling",
                DistanceKm = 5.0,
                EstimatedTime = TimeSpan.FromMinutes(20),
                Waypoints = new List<WaypointImportDto>
                {
                    new WaypointImportDto { Latitude = 48.0, Longitude = 16.0 },
                    new WaypointImportDto { Latitude = 48.1, Longitude = 16.1 }
                }
            };

            Assert.ThrowsAsync<ArgumentException>(async () =>
                await _tourService.ImportToursAsync(user.Id, new List<TourImportDto> { emptyNameDto }));

            // 2. Waypoints < 2
            var missingWaypointsDto = new TourImportDto
            {
                Name = "Invalid",
                TransportType = "cycling",
                DistanceKm = 5.0,
                EstimatedTime = TimeSpan.FromMinutes(20),
                Waypoints = new List<WaypointImportDto>
                {
                    new WaypointImportDto { Latitude = 48.0, Longitude = 16.0 }
                }
            };

            Assert.ThrowsAsync<ArgumentException>(async () =>
                await _tourService.ImportToursAsync(user.Id, new List<TourImportDto> { missingWaypointsDto }));
        }

        [Test]
        public async Task ImportToursAsync_UserIsolation_ShouldOnlyImportForTargetUser()
        {
            if (!await CheckDbConnectionAsync())
            {
                Assert.Ignore("Skipping test: Database not accessible.");
                return;
            }

            var user1 = await CreateTestUserAsync();
            var user2 = await CreateTestUserAsync();

            var importDto = new TourImportDto
            {
                Name = "User 1 Tour",
                TransportType = "cycling",
                DistanceKm = 5.0,
                EstimatedTime = TimeSpan.FromMinutes(20),
                RouteInformation = "geom",
                Waypoints = new List<WaypointImportDto>
                {
                    new WaypointImportDto { Latitude = 48.0, Longitude = 16.0 },
                    new WaypointImportDto { Latitude = 48.1, Longitude = 16.1 }
                }
            };

            // Import for User 1
            await _tourService.ImportToursAsync(user1.Id, new List<TourImportDto> { importDto });

            // Check user 1 has the tour
            var user1Tours = await _tourRepository.GetToursByUserIdAsync(user1.Id);
            Assert.That(user1Tours.Count, Is.EqualTo(1));

            // Check user 2 does not have the tour
            var user2Tours = await _tourRepository.GetToursByUserIdAsync(user2.Id);
            Assert.That(user2Tours.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task ImportToursAsync_InvalidCoordinates_ShouldThrowArgumentException()
        {
            if (!await CheckDbConnectionAsync())
            {
                Assert.Ignore("Skipping test: Database not accessible.");
                return;
            }

            var user = await CreateTestUserAsync();

            // 1. Latitude too high
            var invalidLatDto = new TourImportDto
            {
                Name = "Invalid Lat",
                TransportType = "cycling",
                DistanceKm = 5.0,
                EstimatedTime = TimeSpan.FromMinutes(20),
                Waypoints = new List<WaypointImportDto>
                {
                    new WaypointImportDto { Latitude = 95.0, Longitude = 16.0 },
                    new WaypointImportDto { Latitude = 48.1, Longitude = 16.1 }
                }
            };
            Assert.ThrowsAsync<ArgumentException>(async () =>
                await _tourService.ImportToursAsync(user.Id, new List<TourImportDto> { invalidLatDto }));

            // 2. Longitude too low
            var invalidLonDto = new TourImportDto
            {
                Name = "Invalid Lon",
                TransportType = "cycling",
                DistanceKm = 5.0,
                EstimatedTime = TimeSpan.FromMinutes(20),
                Waypoints = new List<WaypointImportDto>
                {
                    new WaypointImportDto { Latitude = 48.0, Longitude = -185.0 },
                    new WaypointImportDto { Latitude = 48.1, Longitude = 16.1 }
                }
            };
            Assert.ThrowsAsync<ArgumentException>(async () =>
                await _tourService.ImportToursAsync(user.Id, new List<TourImportDto> { invalidLonDto }));
        }

        [Test]
        public async Task ImportToursAsync_TransactionRollback_ShouldNotPersistAnyTourIfOneIsInvalid()
        {
            if (!await CheckDbConnectionAsync())
            {
                Assert.Ignore("Skipping test: Database not accessible.");
                return;
            }

            var user = await CreateTestUserAsync();

            var validDto = new TourImportDto
            {
                Name = "Valid Tour In Batch",
                TransportType = "cycling",
                DistanceKm = 5.0,
                EstimatedTime = TimeSpan.FromMinutes(20),
                RouteInformation = "some-geom",
                Waypoints = new List<WaypointImportDto>
                {
                    new WaypointImportDto { Latitude = 48.0, Longitude = 16.0 },
                    new WaypointImportDto { Latitude = 48.1, Longitude = 16.1 }
                }
            };

            var invalidDto = new TourImportDto
            {
                Name = "", // Invalid: Empty name
                TransportType = "cycling",
                DistanceKm = 5.0,
                EstimatedTime = TimeSpan.FromMinutes(20),
                Waypoints = new List<WaypointImportDto>
                {
                    new WaypointImportDto { Latitude = 48.0, Longitude = 16.0 },
                    new WaypointImportDto { Latitude = 48.1, Longitude = 16.1 }
                }
            };

            var batch = new List<TourImportDto> { validDto, invalidDto };

            // Import should fail due to the second tour being invalid
            Assert.ThrowsAsync<ArgumentException>(async () =>
                await _tourService.ImportToursAsync(user.Id, batch));

            // Verify that the valid tour was NOT inserted (rollback check)
            var toursInDb = await _tourRepository.GetToursByUserIdAsync(user.Id);
            Assert.That(toursInDb.Count, Is.EqualTo(0), "No tours should be saved if any tour in the batch is invalid.");
        }

        [Test]
        public async Task ImportToursAsync_TransactionRollback_ShouldRollbackOnInvalidLog()
        {
            if (!await CheckDbConnectionAsync())
            {
                Assert.Ignore("Skipping test: Database not accessible.");
                return;
            }

            var user = await CreateTestUserAsync();

            var invalidLogDto = new TourImportDto
            {
                Name = "Tour With Invalid Log",
                TransportType = "cycling",
                DistanceKm = 5.0,
                EstimatedTime = TimeSpan.FromMinutes(20),
                RouteInformation = "some-geom",
                Waypoints = new List<WaypointImportDto>
                {
                    new WaypointImportDto { Latitude = 48.0, Longitude = 16.0 },
                    new WaypointImportDto { Latitude = 48.1, Longitude = 16.1 }
                },
                Logs = new List<TourLogImportDto>
                {
                    new TourLogImportDto
                    {
                        Comment = "Invalid difficulty",
                        Difficulty = 10, // Invalid: max is 5
                        Rating = 4,
                        TotalDistanceKm = 5.0,
                        TotalTimeMin = TimeSpan.FromMinutes(20)
                    }
                }
            };

            Assert.ThrowsAsync<ArgumentException>(async () =>
                await _tourService.ImportToursAsync(user.Id, new List<TourImportDto> { invalidLogDto }));

            // Verify nothing is persisted
            var toursInDb = await _tourRepository.GetToursByUserIdAsync(user.Id);
            Assert.That(toursInDb.Count, Is.EqualTo(0), "Tour should not be saved if log difficulty is out of range.");
        }

        [Test]
        public async Task ImportToursAsync_UserIsolation_EnsureToursArePrivate()
        {
            if (!await CheckDbConnectionAsync())
            {
                Assert.Ignore("Skipping test: Database not accessible.");
                return;
            }

            var user1 = await CreateTestUserAsync();
            var user2 = await CreateTestUserAsync();

            var importDto = new TourImportDto
            {
                Name = "Private Tour",
                TransportType = "cycling",
                DistanceKm = 5.0,
                EstimatedTime = TimeSpan.FromMinutes(20),
                RouteInformation = "geom",
                Waypoints = new List<WaypointImportDto>
                {
                    new WaypointImportDto { Latitude = 48.0, Longitude = 16.0 },
                    new WaypointImportDto { Latitude = 48.1, Longitude = 16.1 }
                }
            };

            // Import for User 1
            await _tourService.ImportToursAsync(user1.Id, new List<TourImportDto> { importDto });

            var toursUser1 = await _tourRepository.GetToursByUserIdAsync(user1.Id);
            var importedTour = toursUser1[0];

            // Verify User 2 cannot access this tour via GetToursByUserIdAsync
            var toursUser2 = await _tourRepository.GetToursByUserIdAsync(user2.Id);
            Assert.That(toursUser2.Any(t => t.Id == importedTour.Id), Is.False, "User 2 should not see User 1's tour in search.");
        }
    }
}
