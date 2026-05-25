using Microsoft.AspNetCore.Mvc;
using TourPlanner.BusinessLayer.Dtos;
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
                _logger.LogInformation("GetAllTours was called");

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

                var createdTour = await _tourService.CreateTourAsync(dto);

                return CreatedAtAction(nameof(GetTourById), new { id = createdTour.Id }, createdTour);
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
                var fullMessage = ex.InnerException != null 
                    ? $"{ex.Message} -> {ex.InnerException.Message}" 
                    : ex.Message;
                _logger.LogError(ex, "An unexpected error occurred while creating the tour: {Message}", fullMessage);
                return StatusCode(500, new { message = "Internal Server Error", detail = fullMessage });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTourById(Guid id)
        {
            var tour = await _tourService.GetTourByIdAsync(id);
            if (tour == null)
            {
                return NotFound(new { message = $"Tour with ID {id} not found." });
            }
            return Ok(tour);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTour(Guid id, [FromBody] CreateTourDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _tourService.UpdateTourAsync(id, dto);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                var fullMessage = ex.InnerException != null 
                    ? $"{ex.Message} -> {ex.InnerException.Message}" 
                    : ex.Message;
                _logger.LogError(ex, "Error while updating tour {Id}: {Message}", id, fullMessage);
                return StatusCode(500, new { message = "Internal Server Error", detail = fullMessage });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTour(Guid id)
        {
            try
            {
                await _tourService.DeleteTourAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while deleting tour {Id}", id);
                return StatusCode(500, new { message = "Internal Server Error" });
            }
        }
    }
}