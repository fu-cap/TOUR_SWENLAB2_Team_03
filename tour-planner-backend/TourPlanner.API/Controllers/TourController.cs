using Microsoft.AspNetCore.Mvc;
using TourPlanner.API.Dtos;
using TourPlanner.BusinessLayer.Services;

namespace TourPlanner.API.Controllers
{
    // api/tour
    [Route("api/[controller]")]
    [ApiController] // Aktiviert automatische Features wie die Validierung von Inputs
    public class TourController : ControllerBase
    {
        // Ein Logger, der standardmäßig von ASP.NET mitgeliefert wird
        private readonly ILogger<TourController> _logger;
        private readonly ITourService _tourService;

        public TourController(ITourService tourService, ILogger<TourController> logger)
        {
            _tourService = tourService;
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

        [HttpPost]
        public async Task<IActionResult> CreateTour([FromBody] CreateTourDto dto)
        {
            if(!ModelState.IsValid) return BadRequest(ModelState);

            var createdTour = await _tourService.CreateTourAsync(dto.userId, dto.name, dto.description, dto.from, dto.to, dto.TransportType);

            return CreatedAtAction(nameof(CreateTour), new { id = createdTour.Id }, createdTour);
        }

    }
}