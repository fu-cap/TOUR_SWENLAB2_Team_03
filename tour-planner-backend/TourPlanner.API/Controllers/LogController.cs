using Microsoft.AspNetCore.Mvc;
using TourPlanner.BusinessLayer.Dtos;
using TourPlanner.BusinessLayer.Services;

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
            _logger.LogInformation("GetAllLogs called");
            try
            {
                var logs = await _logService.GetAllLogsAsync();
                return Ok(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching all logs.");
                return StatusCode(500, new { message = "Internal Server Error" });
            }
        }

        [HttpGet("{logId:guid}")]
        public async Task<IActionResult> GetLogById([FromRoute] Guid logId)
        {
            _logger.LogInformation("GetLogById called for log: {LogId}", logId);
            try
            {
                var log = await _logService.GetLogByIdAsync(logId);
                if (log is null)
                {
                    return NotFound(new { message = "Log not found" });
                }
                return Ok(log);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting log {LogId}", logId);
                return StatusCode(500, new { message = "Internal Server Error" });
            }
        }

        [HttpGet("tour/{tourId:guid}")]
        public async Task<IActionResult> GetLogsByTourId([FromRoute] Guid tourId)
        {
            _logger.LogInformation("GetLogsByTourId called for tour: {TourId}", tourId);
            try
            {
                var logs = await _logService.GetLogsByTourIdAsync(tourId);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting logs for tour {TourId}", tourId);
                return StatusCode(500, new { message = "Internal Server Error" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateLog([FromBody] CreateLogDto dto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Got invalid DTO for creating a log.");
                return BadRequest(ModelState);
            }

            try
            {
                _logger.LogInformation("Creating log for tour: {TourId}", dto.TourId);
                var createdLog = await _logService.CreateLogAsync(dto);
                return CreatedAtAction(nameof(GetLogById), new { logId = createdLog.Id }, createdLog);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Tour not found when creating log for tour {TourId}", dto.TourId);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating log for tour {TourId}", dto.TourId);
                return StatusCode(500, new { message = "Internal Server Error" });
            }
        }

        [HttpPut("{logId:guid}")]
        public async Task<IActionResult> UpdateLogAsync([FromRoute] Guid logId, [FromBody] CreateLogDto dto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Got invalid DTO for updating a log.");
                return BadRequest(ModelState);
            }

            try
            {
                _logger.LogInformation("Updating log {LogId} for tour: {TourId}", logId, dto.TourId);
                await _logService.UpdateLogAsync(logId, dto);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Tour or log not found when updating log {LogId}", logId);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while updating log {LogId}", logId);
                return StatusCode(500, new { message = "Internal Server Error" });
            }
        }

        [HttpDelete("{logId:guid}")]
        public async Task<IActionResult> DeleteLogAsync([FromRoute] Guid logId)
        {
            try
            {
                _logger.LogInformation("Deleting log: {LogId}", logId);
                await _logService.DeleteLogAsync(logId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Log {LogId} not found when deleting", logId);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while deleting log {LogId}", logId);
                return StatusCode(500, new { message = "Internal Server Error" });
            }
        }
    }
}
