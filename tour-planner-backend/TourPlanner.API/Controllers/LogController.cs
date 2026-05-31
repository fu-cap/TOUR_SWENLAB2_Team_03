using Microsoft.AspNetCore.Mvc;
using TourPlanner.BusinessLayer.Dtos;
using TourPlanner.BusinessLayer.Services;
using TourPlanner.DataAccessLayer.Entities;

namespace TourPlanner.API.Controllers
{
    //api/log
    [Route("api/[controller]")]
    [ApiController]
    public class LogController: ControllerBase
    {
        private readonly ILogger<LogController> _logger;
        private readonly ILogService _logService;

        public LogController(ILogService logService, ILogger<LogController> logger)
        {
            _logService = logService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllLogs()
        {
            _logger.LogInformation("Get All Logs called");
            try
            {
                var logs = await _logService.GetAllLogsAsync();
                return Ok(logs);
            }
            catch (NotImplementedException ex)
            {
                _logger.LogError(ex, "Error while exporting tour logs.");
                return StatusCode(500, new { message = "Internal Server Error" });  
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while exporting tour logs.");
                return StatusCode(500, new { message = "Internal Server Error" });  
            }
        }

        [HttpGet("{tourId:guid}")]
        public async Task<IActionResult> GetLogsByTourId([FromRoute] Guid tourId)
        {
            _logger.LogInformation("Get Logs by Tour Id called for tour: {Tour}", tourId);
            try
            {
                var logs = await _logService.GetLogsByTourIdAsync(tourId);
                if(logs is null)
                {
                    return NotFound(new { message = "Tour not found" });
                }
                return Ok(logs);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogError(ex, "Tour was not found when getting logs by tour id");
                return StatusCode(404, new { message = "Tour not found" });  
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting logs by tour id");
                return StatusCode(500, new { message = "Internal Server Error" });  
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateLog([FromBody] CreateLogDto dto)
        {
            if (!ModelState.IsValid) 
            {
                _logger.LogWarning("Got invalid DTO for creating a user.");
                return BadRequest(ModelState);
            }

            try
            {
                _logger.LogInformation("Starting creating log for tour: {Tour}", dto.tour_id);

                var createdLog = await _logService.CreateLogAsync(dto);

                if(createdLog is not null)
                {
                    return Created();
                }
                throw new Exception("Returned user is null");

            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogError(ex, "Tour was not found when creating new log");
                return StatusCode(404, new { message = "Tour not found" });  
            }
            catch (IndexOutOfRangeException ex)
            {
                _logger.LogError(ex, "Error when creating log");
                return StatusCode(400, new { message = ex });  
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating new log");
                return StatusCode(500, new { message = "Internal Server Error" });  
            }
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteLog(Guid id)
        {
            _logger.LogInformation("Delete Log called for id: {Id}", id);
            try
            {
                await _logService.DeleteLogAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogError(ex, "Log was not found when deleting");
                return StatusCode(404, new { message = "Log not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while deleting log");
                return StatusCode(500, new { message = "Internal Server Error" });
            }
        }

    }
}