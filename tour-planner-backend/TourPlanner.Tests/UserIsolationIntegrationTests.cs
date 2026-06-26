using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using TourPlanner.DataAccessLayer;
using TourPlanner.DataAccessLayer.Entities;
using TourPlanner.DataAccessLayer.Enums;
using TourPlanner.DataAccessLayer.Repositories;

namespace TourPlanner.Tests
{
    [TestFixture]
    public class UserIsolationIntegrationTests
    {
        private TourPlannerDbContext _context = null!;

        [SetUp]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<TourPlannerDbContext>()
                .UseNpgsql("Host=localhost;Database=tour_planner;Username=postgres;Password=password")
                .Options;

            _context = new TourPlannerDbContext(options);
        }

        [TearDown]
        public void TearDown()
        {
            _context?.Dispose();
        }

        [Test]
        public async Task VerifyUserIsolationInSearch()
        {
            bool canConnect = false;
            try
            {
                canConnect = await _context.Database.CanConnectAsync();
            }
            catch { /* connection refused */ }

            if (!canConnect)
            {
                Assert.Ignore("Skipping integration test: Postgres database is not accessible.");
                return;
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Create User A
                var userA = new User
                {
                    Id = Guid.NewGuid(),
                    Username = $"usera_{Guid.NewGuid()}",
                    Email = $"usera_{Guid.NewGuid()}@example.com",
                    PasswordHash = "hashed",
                    Gender = "Male",
                    FirstName = "User",
                    LastName = "A"
                };
                _context.Users.Add(userA);

                // 2. Create User B
                var userB = new User
                {
                    Id = Guid.NewGuid(),
                    Username = $"userb_{Guid.NewGuid()}",
                    Email = $"userb_{Guid.NewGuid()}@example.com",
                    PasswordHash = "hashed",
                    Gender = "Female",
                    FirstName = "User",
                    LastName = "B"
                };
                _context.Users.Add(userB);

                // 3. Create Tour A (for User A)
                var tourA = new Tour
                {
                    Id = Guid.NewGuid(),
                    UserId = userA.Id,
                    Name = "User A Special Alpine Hike",
                    Description = "Scenic alpine views and fresh mountain air.",
                    TransportType = TransportType.FootHiking,
                    DistanceKm = 15.50,
                    EstimatedTime = TimeSpan.FromMinutes(240),
                    Popularity = 5.0,
                    ChildFriendliness = 4.0,
                    CreationDate = DateTime.UtcNow,
                    LastModifiedDate = DateTime.UtcNow
                };
                _context.Tours.Add(tourA);

                // 4. Create Tour B (for User B)
                var tourB = new Tour
                {
                    Id = Guid.NewGuid(),
                    UserId = userB.Id,
                    Name = "User B Secret Forest Trail",
                    Description = "Quiet nature trail through deep pine forest.",
                    TransportType = TransportType.FootWalking,
                    DistanceKm = 8.20,
                    EstimatedTime = TimeSpan.FromMinutes(90),
                    Popularity = 3.0,
                    ChildFriendliness = 9.0,
                    CreationDate = DateTime.UtcNow,
                    LastModifiedDate = DateTime.UtcNow
                };
                _context.Tours.Add(tourB);

                await _context.SaveChangesAsync();

                // 5. Create Tour Log A (for Tour A)
                var logA = new Log
                {
                    Id = Guid.NewGuid(),
                    TourId = tourA.Id,
                    DateTime = DateTime.UtcNow,
                    Comment = "This is a great commonpath experience with User A details.",
                    Difficulty = 3,
                    TotalDistanceKm = 15.50,
                    TotalTimeMin = TimeSpan.FromMinutes(240),
                    Rating = 5,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Log.Add(logA);

                // 6. Create Tour Log B (for Tour B)
                var logB = new Log
                {
                    Id = Guid.NewGuid(),
                    TourId = tourB.Id,
                    DateTime = DateTime.UtcNow,
                    Comment = "This is a simple commonpath experience with User B secrets.",
                    Difficulty = 1,
                    TotalDistanceKm = 8.20,
                    TotalTimeMin = TimeSpan.FromMinutes(90),
                    Rating = 4,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Log.Add(logB);

                await _context.SaveChangesAsync();

                var repository = new TourRepository(_context);

                // --- TEST ISOLATION ON TOURS AND LOGS ---

                // Test Case 1: Search for common word ("commonpath")
                // User A should only see Tour A (and not Tour B)
                var searchA_Common = await repository.GetToursByUserIdAsync(userA.Id, "commonpath");
                Assert.That(searchA_Common.Count, Is.EqualTo(1), "User A searching for 'commonpath' should only return User A's tour.");
                Assert.That(searchA_Common[0].Id, Is.EqualTo(tourA.Id), "User A's search result should match Tour A.");

                // User B should only see Tour B (and not Tour A)
                var searchB_Common = await repository.GetToursByUserIdAsync(userB.Id, "commonpath");
                Assert.That(searchB_Common.Count, Is.EqualTo(1), "User B searching for 'commonpath' should only return User B's tour.");
                Assert.That(searchB_Common[0].Id, Is.EqualTo(tourB.Id), "User B's search result should match Tour B.");

                // Test Case 2: Search for unique User A log comment content
                // User A should find Tour A
                var searchA_Unique = await repository.GetToursByUserIdAsync(userA.Id, "details");
                Assert.That(searchA_Unique.Count, Is.EqualTo(1), "User A searching for 'details' (from User A log) should return Tour A.");

                // User B should NOT find Tour B or Tour A
                var searchB_UniqueA = await repository.GetToursByUserIdAsync(userB.Id, "details");
                Assert.That(searchB_UniqueA.Count, Is.EqualTo(0), "User B searching for User A's unique log content ('details') must return 0 results.");

                // Test Case 3: Search for unique User B log comment content
                // User B should find Tour B
                var searchB_Unique = await repository.GetToursByUserIdAsync(userB.Id, "secrets");
                Assert.That(searchB_Unique.Count, Is.EqualTo(1), "User B searching for 'secrets' (from User B log) should return Tour B.");

                // User A should NOT find Tour A or Tour B
                var searchA_UniqueB = await repository.GetToursByUserIdAsync(userA.Id, "secrets");
                Assert.That(searchA_UniqueB.Count, Is.EqualTo(0), "User A searching for User B's unique log content ('secrets') must return 0 results.");

                // Test Case 4: General all-tours retrieve (empty search)
                var allA = await repository.GetToursByUserIdAsync(userA.Id, "");
                Assert.That(allA.Count, Is.EqualTo(1), "User A empty search should return exactly 1 tour.");
                Assert.That(allA[0].Id, Is.EqualTo(tourA.Id));

                var allB = await repository.GetToursByUserIdAsync(userB.Id, "");
                Assert.That(allB.Count, Is.EqualTo(1), "User B empty search should return exactly 1 tour.");
                Assert.That(allB[0].Id, Is.EqualTo(tourB.Id));
            }
            finally
            {
                await transaction.RollbackAsync();
            }
        }
    }
}
