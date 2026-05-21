using Microsoft.AspNetCore.Mvc;
using TourPlanner.API.Dtos;
using TourPlanner.BusinessLayer.Services;
using TourPlanner.DataAccessLayer.Entities;

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
        public async Task<IActionResult> GetAllTours()
        {
            try
            {
                _logger.LogInformation("GetAllTours wurde aufgerufen.");

                List<Tour> allTours = await _tourService.GetAllToursAsync();

                return Ok(allTours);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while exporting tours.");
                return StatusCode(500, new {message = "Export failed"});
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateTour([FromBody] CreateTourDto dto)
        {
            if (!ModelState.IsValid) 
            {
                _logger.LogWarning("Got invalid DTO for creating a tour.");
                return BadRequest(ModelState);
            }

            try{
                _logger.LogInformation("Starting creating tour: {TourName}", dto.name);

                var createdTour = await _tourService.CreateTourAsync(dto.userId, dto.name, dto.description, dto.from, dto.to, dto.TransportType);

                return CreatedAtAction(nameof(CreateTour), new { id = createdTour.Id }, createdTour);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Error in Business Logic: {TourName}", dto.name);
                return BadRequest(new { message = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error while communicating with external API.");
                return StatusCode(503, new { message = "The route service is unavailable" });
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while creating the tour");
                return StatusCode(500, new { message = "Internal Server Error" });
            }
        }

    }
}