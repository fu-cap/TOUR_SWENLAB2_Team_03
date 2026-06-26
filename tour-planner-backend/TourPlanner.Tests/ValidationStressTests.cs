using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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
    public class ValidationStressTests
    {
        private TourPlannerDbContext _context = null!;

        [SetUp]
        public void SetUp()
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
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
        [TestCase(-100)]
        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(100)]
        public void CreateLogDto_InvalidDifficulty_ShouldFailValidation(int difficulty)
        {
            var dto = new CreateLogDto
            {
                TourId = Guid.NewGuid(),
                DateTime = DateTime.UtcNow,
                Comment = "Stress test difficulty",
                Difficulty = difficulty,
                Rating = 3
            };

            var context = new ValidationContext(dto, null, null);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(dto, context, results, true);

            Assert.That(isValid, Is.False, $"Difficulty {difficulty} should be invalid.");
            Assert.That(results.Any(r => r.MemberNames.Contains("Difficulty")), Is.True);
        }

        [Test]
        [TestCase(-100)]
        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(100)]
        public void CreateLogDto_InvalidRating_ShouldFailValidation(int rating)
        {
            var dto = new CreateLogDto
            {
                TourId = Guid.NewGuid(),
                DateTime = DateTime.UtcNow,
                Comment = "Stress test rating",
                Difficulty = 3,
                Rating = rating
            };

            var context = new ValidationContext(dto, null, null);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(dto, context, results, true);

            Assert.That(isValid, Is.False, $"Rating {rating} should be invalid.");
            Assert.That(results.Any(r => r.MemberNames.Contains("Rating")), Is.True);
        }

        [Test]
        public async Task DatabaseDirectInsert_BypassValidation_BehaviorCheck()
        {
            bool canConnect = false;
            try
            {
                canConnect = await _context.Database.CanConnectAsync();
            }
            catch { /* connection refused */ }

            if (!canConnect)
            {
                Assert.Ignore("Skipping DB direct insert test: Postgres database is not accessible.");
                return;
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Create a test user and tour to link logs to
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Username = $"valuser_{Guid.NewGuid()}",
                    Email = $"val_{Guid.NewGuid()}@example.com",
                    PasswordHash = "hashed",
                    Gender = "Other",
                    FirstName = "Val",
                    LastName = "Test"
                };
                _context.Users.Add(user);

                var tour = new Tour
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Name = "Validation Test Tour",
                    Description = "Tour to test DB level constraints",
                    TransportType = TransportType.FootWalking,
                    DistanceKm = 5.0,
                    EstimatedTime = TimeSpan.FromHours(1),
                    Popularity = 0.0,
                    ChildFriendliness = 10.0,
                    CreationDate = DateTime.UtcNow,
                    LastModifiedDate = DateTime.UtcNow
                };
                _context.Tours.Add(tour);
                await _context.SaveChangesAsync();

                // Now attempt to insert a log directly with an invalid difficulty (e.g. 99)
                var invalidLog = new Log
                {
                    Id = Guid.NewGuid(),
                    TourId = tour.Id,
                    DateTime = DateTime.UtcNow,
                    Comment = "Direct DB insert with invalid difficulty",
                    Difficulty = 99, // Out of DTO range [1..5]
                    Rating = 99,     // Out of DTO range [1..5]
                    TotalDistanceKm = 5.0,
                    TotalTimeMin = TimeSpan.FromHours(1),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Log.Add(invalidLog);
                
                // Let's see if the database throws a DbUpdateException (e.g. due to check constraint) or succeeds.
                // Note: If there are no DB-level check constraints, the save will succeed, which is acceptable
                // because the validation is handled at the presentation/API level, but we want to log the behavior.
                try
                {
                    await _context.SaveChangesAsync();
                    Console.WriteLine("[INFO] Database successfully inserted invalid difficulty/rating (no DB-level check constraints exist).");
                }
                catch (DbUpdateException dbEx)
                {
                    var innerMsg = dbEx.InnerException?.Message ?? "No inner exception";
                    Console.WriteLine($"[INFO] Database blocked direct invalid insert as expected. Exception: {dbEx.Message}. Inner Exception: {innerMsg}");
                }
            }
            finally
            {
                await transaction.RollbackAsync();
            }
        }
    }
}
