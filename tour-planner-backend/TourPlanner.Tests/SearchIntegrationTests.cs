using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NUnit.Framework;
using TourPlanner.DataAccessLayer;
using TourPlanner.DataAccessLayer.Entities;
using TourPlanner.DataAccessLayer.Enums;
using TourPlanner.DataAccessLayer.Repositories;

namespace TourPlanner.Tests
{
    [TestFixture]
    public class SearchIntegrationTests
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
        public async Task VerifyDatabaseConnectionAndSearch()
        {
            bool canConnect = false;
            try { canConnect = await _context.Database.CanConnectAsync(); } catch { }
            if (!canConnect)
            {
                Assert.Ignore("Skipping integration test: Postgres database is not accessible on localhost:5432.");
                return;
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Create a test user
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Username = $"testuser_{Guid.NewGuid()}",
                    Email = $"test_{Guid.NewGuid()}@example.com",
                    PasswordHash = "hashed",
                    Gender = "Other",
                    FirstName = "Test",
                    LastName = "User"
                };
                _context.Users.Add(user);

                // Create a test tour
                var tour = new Tour
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Name = "Vienna Forest Walk",
                    Description = "A nice walk in the Vienna woods with a beautiful lake",
                    TransportType = TransportType.FootWalking,
                    DistanceKm = 12.34,
                    EstimatedTime = TimeSpan.FromMinutes(120),
                    Popularity = 4.50,
                    ChildFriendliness = 8.20,
                    CreationDate = DateTime.UtcNow,
                    LastModifiedDate = DateTime.UtcNow
                };
                _context.Tours.Add(tour);

                // Create a test tour with literal wildcard characters
                var wildcardTour = new Tour
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Name = "Special Tour 100%_Free",
                    Description = "A tour containing literal % and _ characters",
                    TransportType = TransportType.FootWalking,
                    DistanceKm = 5.00,
                    EstimatedTime = TimeSpan.FromMinutes(60),
                    Popularity = 1.00,
                    ChildFriendliness = 10.00,
                    CreationDate = DateTime.UtcNow,
                    LastModifiedDate = DateTime.UtcNow
                };
                _context.Tours.Add(wildcardTour);

                await _context.SaveChangesAsync();

                var repository = new TourRepository(_context);

                // 1. Simple search matching name
                var results = await repository.GetToursByUserIdAsync(user.Id, "Vienna");
                Assert.That(results.Count, Is.EqualTo(1), "Search for 'Vienna' should match.");

                // 2. Search matching description
                results = await repository.GetToursByUserIdAsync(user.Id, "lake");
                Assert.That(results.Count, Is.EqualTo(1), "Search for description content 'lake' should match.");

                // 3. Empty search
                results = await repository.GetToursByUserIdAsync(user.Id, "");
                Assert.That(results.Count, Is.EqualTo(2), "Empty search should return all user's tours.");

                // 4. Non-existent term
                results = await repository.GetToursByUserIdAsync(user.Id, "nonexistentterm123");
                Assert.That(results.Count, Is.EqualTo(0), "Search for non-existent term should return zero results.");

                // 5. Wildcard character '%'
                results = await repository.GetToursByUserIdAsync(user.Id, "%");
                Console.WriteLine($"[Result] Search for '%' matched: {results.Count} (Expected: 1 if literal match, 2 if unescaped wildcard)");
                Assert.That(results.Count, Is.EqualTo(1), "Search for '%' should only match the tour with a literal '%' character.");
                Assert.That(results[0].Id, Is.EqualTo(wildcardTour.Id), "Matched tour should be the wildcardTour.");

                // 6. Wildcard character '_'
                results = await repository.GetToursByUserIdAsync(user.Id, "_");
                Console.WriteLine($"[Result] Search for '_' matched: {results.Count} (Expected: 1 if literal match, 2 if unescaped wildcard)");
                Assert.That(results.Count, Is.EqualTo(1), "Search for '_' should only match the tour with a literal '_' character.");
                Assert.That(results[0].Id, Is.EqualTo(wildcardTour.Id), "Matched tour should be the wildcardTour.");

                // 7. Numeric distance search
                results = await repository.GetToursByUserIdAsync(user.Id, "12.34");
                Assert.That(results.Count, Is.EqualTo(1), "Search for distance '12.34' should match.");

                // 8. Numeric popularity search
                results = await repository.GetToursByUserIdAsync(user.Id, "4.50");
                Assert.That(results.Count, Is.EqualTo(1), "Search for popularity '4.50' should match.");

                // 9. German/Austrian comma separator search
                results = await repository.GetToursByUserIdAsync(user.Id, "12,34");
                Console.WriteLine($"[Result] Search for '12,34' matched: {results.Count} (Expected: 1 if localized decimal parsing, 0 if raw text comparison)");

                // 10. Very long search query
                string veryLongSearch = new string('a', 5000);
                results = await repository.GetToursByUserIdAsync(user.Id, veryLongSearch);
                Assert.That(results.Count, Is.EqualTo(0), "Very long query should return zero results without crashing.");

                // 11. websearch_to_tsquery exclusion operator (-)
                results = await repository.GetToursByUserIdAsync(user.Id, "Vienna -Forest");
                Console.WriteLine($"[Result] Search for 'Vienna -Forest' matched: {results.Count} (Expected: 0 because 'Forest' is excluded but present)");

                // 12. websearch_to_tsquery logical OR
                results = await repository.GetToursByUserIdAsync(user.Id, "Vienna OR Bratislava");
                Assert.That(results.Count, Is.EqualTo(1), "Search with 'OR' should match Vienna.");

                // 13. SQL Injection check
                string maliciousSearch = "Hacked' OR 1=1;--";
                results = await repository.GetToursByUserIdAsync(user.Id, maliciousSearch);
                Console.WriteLine($"[Result] Malicious query matched: {results.Count} (Expected: 0 if properly parameterized, >1 or crash if SQL injection vulnerability)");
                Assert.That(results.Count, Is.EqualTo(0), "SQL Injection query should be parameterized and result in 0 matches.");
            }
            finally
            {
                await transaction.RollbackAsync();
            }
        }

        [Test]
        public async Task ProbeSearchExecutionAndEdgeCases()
        {
            bool canConnect = false;
            try { canConnect = await _context.Database.CanConnectAsync(); } catch { }
            if (!canConnect)
            {
                Assert.Ignore("Skipping integration test: Postgres database is not accessible on localhost:5432.");
                return;
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Create a test user
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Username = $"probeuser_{Guid.NewGuid()}",
                    Email = $"probe_{Guid.NewGuid()}@example.com",
                    PasswordHash = "hashed",
                    Gender = "Other",
                    FirstName = "Probe",
                    LastName = "User"
                };
                _context.Users.Add(user);

                // Create a test tour
                var tour = new Tour
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Name = "Vienna Forest Walk",
                    Description = "A nice walk in the Vienna woods with a beautiful lake",
                    TransportType = TransportType.FootWalking,
                    DistanceKm = 12.34,
                    EstimatedTime = TimeSpan.FromMinutes(120),
                    Popularity = 4.50,
                    ChildFriendliness = 8.20,
                    CreationDate = DateTime.UtcNow,
                    LastModifiedDate = DateTime.UtcNow
                };
                _context.Tours.Add(tour);

                await _context.SaveChangesAsync();

                var repository = new TourRepository(_context);

                // 1. Probe SQL Execution Plan (Performance)
                Console.WriteLine("=== PROBING POSTGRES EXPLAIN PLAN FOR SEARCH QUERY ===");
                try
                {
                    var searchTerm = "Vienna";
                    var searchLike = $"%{searchTerm}%";
                    
                    using var command = _context.Database.GetDbConnection().CreateCommand();
                    command.Transaction = _context.Database.CurrentTransaction?.GetDbTransaction();
                    command.CommandText = @"
                        EXPLAIN 
                        SELECT tour_id 
                        FROM v_tour_search 
                        WHERE user_id = @userId 
                          AND (
                              search_vector @@ websearch_to_tsquery('english', @search)
                              OR name ILIKE @likeSearch
                              OR description ILIKE @likeSearch
                              OR distance_km::text ILIKE @likeSearch
                              OR estimated_time_min::text ILIKE @likeSearch
                              OR popularity::text ILIKE @likeSearch
                              OR child_friendliness::text ILIKE @likeSearch
                          )";
                    
                    var pUserId = command.CreateParameter();
                    pUserId.ParameterName = "@userId";
                    pUserId.Value = user.Id;
                    command.Parameters.Add(pUserId);

                    var pSearch = command.CreateParameter();
                    pSearch.ParameterName = "@search";
                    pSearch.Value = searchTerm;
                    command.Parameters.Add(pSearch);

                    var pLike = command.CreateParameter();
                    pLike.ParameterName = "@likeSearch";
                    pLike.Value = searchLike;
                    command.Parameters.Add(pLike);

                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        Console.WriteLine(reader.GetString(0));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not retrieve EXPLAIN plan: {ex.Message}");
                }

                // 2. Probe various edge cases and special characters
                var testInputs = new[]
                {
                    "",                         // Empty
                    " ",                        // Whitespace
                    new string('x', 1000),      // Long input
                    "%",                        // Wildcard percent
                    "_",                        // Wildcard underscore
                    "Vienna%",                  // Wildcard combined
                    "Vienna_",                  // Wildcard combined
                    "Vienna*",                  // Asterisk
                    "Vienna?",                  // Question mark
                    "Vienna!",                  // Exclamation
                    "Vienna\"",                 // Single double quote
                    "\"Vienna\"",               // Quoted phrase
                    "Vienna'",                  // Single single quote
                    "Vienna\\",                 // Backslash
                    "Vienna/",                  // Forward slash
                    "Vienna-Forest",            // Dash (exclusion in websearch)
                    "Vienna -Forest",           // Space dash (exclusion in websearch)
                    "Vienna OR Bratislava",     // Logical OR
                    "Vienna AND Forest",        // Logical AND
                    "Vienna & Forest",          // Ampersand
                    "Vienna | Forest",          // Pipe
                    "Vienna' OR 1=1;--",        // Malicious injection attempt (matches Vienna due to websearch)
                    "nonexistent' OR 1=1;--",   // Malicious injection attempt (should match 0)
                    "'; SELECT 1; --",          // Malicious injection attempt (should match 0)
                    "'; DROP TABLE tour; --"    // Malicious injection attempt (should match 0)
                };

                Console.WriteLine("=== PROBING EDGE CASES & SPECIAL CHARACTERS ===");
                foreach (var input in testInputs)
                {
                    try
                    {
                        var results = await repository.GetToursByUserIdAsync(user.Id, input);
                        Console.WriteLine($"Input: '{input.Replace("\n", "\\n")}' -> Matched: {results.Count} (Succeeded without exception)");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Input: '{input}' -> FAILED with exception: {ex.GetType().Name}: {ex.Message}");
                        Assert.Fail($"Query threw exception for input '{input}': {ex.Message}");
                    }
                }
            }
            finally
            {
                await transaction.RollbackAsync();
            }
        }
    }
}
