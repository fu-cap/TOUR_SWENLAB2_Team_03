using System;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using TourPlanner.BusinessLayer.Dtos;
using TourPlanner.BusinessLayer.Services;
using TourPlanner.BusinessLayer.Utils;
using TourPlanner.DataAccessLayer;
using TourPlanner.DataAccessLayer.Entities;
using TourPlanner.DataAccessLayer.Enums;
using TourPlanner.DataAccessLayer.Repositories;

namespace TourPlanner.Tests
{
    [TestFixture]
    public class TourMetricsIntegrationTests
    {
        private TourPlannerDbContext _context = null!;
        private TourRepository _tourRepository = null!;
        private LogRepository _logRepository = null!;
        private LogService _logService = null!;

        [SetUp]
        public void SetUp()
        {
            // Set legacy timestamp behavior switch for PostgreSQL compatibility
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            var options = new DbContextOptionsBuilder<TourPlannerDbContext>()
                .UseNpgsql("Host=localhost;Database=tour_planner;Username=postgres;Password=password")
                .Options;

            _context = new TourPlannerDbContext(options);
            _tourRepository = new TourRepository(_context);
            _logRepository = new LogRepository(_context);
            _logService = new LogService(_logRepository, _tourRepository);
        }

        [TearDown]
        public void TearDown()
        {
            _context?.Dispose();
        }

        [Test]
        public async Task VerifyDynamicMetricsRecalculation_OnLogCrudOperations()
        {
            bool canConnect = false;
            try { canConnect = await _context.Database.CanConnectAsync(); } catch { }
            if (!canConnect)
            {
                Assert.Ignore("Skipping integration test: Postgres database is not accessible.");
                return;
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Create a test user
                var shortGuid = Guid.NewGuid().ToString().Substring(0, 8);
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Username = $"user_{shortGuid}",
                    Email = $"user_{shortGuid}@example.com",
                    PasswordHash = "hashed",
                    Gender = "Other",
                    FirstName = "Test",
                    LastName = "User"
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // 2. Create a test tour
                var tour = new Tour
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Name = "Vienna Forest Walk",
                    Description = "A nice walk",
                    TransportType = TransportType.FootWalking,
                    DistanceKm = 2.0,
                    EstimatedTime = TimeSpan.FromMinutes(30),
                    Popularity = 0.0,
                    ChildFriendliness = 10.0, // calculated for 2.0km / 30min with no logs
                    CreationDate = DateTime.UtcNow,
                    LastModifiedDate = DateTime.UtcNow
                };
                await _tourRepository.AddAsync(tour);

                // Verify initial metrics in database
                var dbTourInitial = await _tourRepository.GetByIdAsync(tour.Id);
                Assert.That(dbTourInitial, Is.Not.Null);
                Assert.That(dbTourInitial!.Popularity, Is.EqualTo(0.0));
                Assert.That(dbTourInitial.ChildFriendliness, Is.EqualTo(10.0));

                // 3. Create a log (CRUD - Create)
                // Let's create a very long and difficult log to cause penalties:
                // Difficulty 5, Distance 10 km, Time 120 mins
                var createLogDto = new CreateLogDto
                {
                    TourId = tour.Id,
                    DateTime = DateTime.UtcNow,
                    Comment = "Very difficult hike",
                    Difficulty = 5,
                    TotalDistanceKm = 10.0,
                    TotalTimeMin = TimeSpan.FromMinutes(120),
                    Rating = 3
                };

                var createdLog = await _logService.CreateLogAsync(createLogDto);
                Assert.That(createdLog, Is.Not.Null);

                // Fetch parent tour from DB to verify propagation
                _context.ChangeTracker.Clear();
                var dbTourAfterCreate = await _tourRepository.GetByIdAsync(tour.Id);
                Assert.That(dbTourAfterCreate, Is.Not.Null);
                
                // Popularity should now be 1.0
                Assert.That(dbTourAfterCreate!.Popularity, Is.EqualTo(1.0));
                
                // Let's check child-friendliness calculation:
                // baseScore for FootWalking = 10.0
                // difficulty penalty = (5.0 - 1.0) * 1.5 = 6.0
                // distance penalty = (10.0 - 3.0) * 0.5 = 3.5
                // time penalty = (120.0 - 30.0) * 0.05 = 4.5
                // childFriendliness = 10.0 - 6.0 - 3.5 - 4.5 = -4.0, clamped to 0.0
                Assert.That(dbTourAfterCreate.ChildFriendliness, Is.EqualTo(0.0));

                // 4. Update the log (CRUD - Update)
                // Let's make the log extremely child-friendly:
                // Difficulty 1, Distance 2 km, Time 20 mins
                var updateLogDto = new CreateLogDto
                {
                    TourId = tour.Id,
                    DateTime = DateTime.UtcNow,
                    Comment = "Super easy recovery walk",
                    Difficulty = 1,
                    TotalDistanceKm = 2.0,
                    TotalTimeMin = TimeSpan.FromMinutes(20),
                    Rating = 5
                };

                _context.ChangeTracker.Clear();
                await _logService.UpdateLogAsync(createdLog.Id, updateLogDto);

                _context.ChangeTracker.Clear();
                var dbTourAfterUpdate = await _tourRepository.GetByIdAsync(tour.Id);
                Assert.That(dbTourAfterUpdate, Is.Not.Null);

                // Popularity should remain 1.0
                Assert.That(dbTourAfterUpdate!.Popularity, Is.EqualTo(1.0));

                // Child friendliness calculated with the updated log:
                // baseScore = 10.0
                // difficulty penalty = (1.0 - 1.0) * 1.5 = 0.0
                // distance penalty = 2.0 <= 3.0 -> 0.0
                // time penalty = 20.0 <= 30.0 -> 0.0
                // childFriendliness = 10.0
                Assert.That(dbTourAfterUpdate.ChildFriendliness, Is.EqualTo(10.0));

                // 5. Delete the log (CRUD - Delete)
                _context.ChangeTracker.Clear();
                await _logService.DeleteLogAsync(createdLog.Id);

                // Verify that the log is no longer in the DB
                _context.ChangeTracker.Clear();
                var deletedLogFromDb = await _logRepository.GetByIdAsync(createdLog.Id);
                Assert.That(deletedLogFromDb, Is.Null, "The log must be completely deleted from the database.");

                // Fetch parent tour to verify it reverted back to default unlogged metrics
                var dbTourAfterDelete = await _tourRepository.GetByIdAsync(tour.Id);
                Assert.That(dbTourAfterDelete, Is.Not.Null);

                // Popularity should be 0.0
                Assert.That(dbTourAfterDelete!.Popularity, Is.EqualTo(0.0));

                // Child friendliness should revert to 10.0
                Assert.That(dbTourAfterDelete.ChildFriendliness, Is.EqualTo(10.0));
            }
            finally
            {
                await transaction.RollbackAsync();
            }
        }
    }
}
