using Microsoft.AspNetCore.Mvc;

namespace TourPlanner.API.Controllers
{
    // api/tour
    [Route("api/[controller]")]
    [ApiController] // Aktiviert automatische Features wie die Validierung von Inputs
    public class TourController : ControllerBase
    {
        // Ein Logger, der standardmäßig von ASP.NET mitgeliefert wird
        private readonly ILogger<TourController> _logger;

        public TourController(ILogger<TourController> logger)
        {
            _logger = logger;
        }

        // GET http://localhost:<port>/api/tour
        [HttpGet]
        public IActionResult GetAllTours()
        {
            _logger.LogInformation("GetAllTours wurde aufgerufen.");

            // Test Data
            var testTours = new[]
            {
                new { Id = 1, Name = "Donauinsel Radtour", Description = "Schöne flache Strecke" },
                new { Id = 2, Name = "Kahlenberg Wanderung", Description = "Steiler Aufstieg mit Aussicht" }
            };

            return Ok(testTours);
        }

    }
}