using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
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

        [HttpGet("{logId:guid}")]
        public async Task<IActionResult> GetLogById([FromRoute] Guid logId)
        {
            _logger.LogInformation("Get Log by Log ID called for log: {Log}", logId);

            try
            {
                var log = await _logService.GetLogByIdAsync(logId);
                if(log is null)
                {
                    return NotFound(new { message = "Log not found" });
                }
                return Ok(log);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogError(ex, "Tour was not found when getting logs by tour id");
                return StatusCode(404, new { message = "Log not found" });  
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting logs by tour id");
                return StatusCode(500, new { message = "Internal Server Error" });  
            }
        }

        [HttpGet("tour/{tourId:guid}")]
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
                throw new Exception("Returned log is null");

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

        [HttpPut("{logId:guid}")]
        public async Task<IActionResult> updateLogAsync([FromRoute] Guid logId, [FromBody] CreateLogDto dto)
        {
             if (!ModelState.IsValid) 
            {
                _logger.LogWarning("Got invalid DTO for creating a user.");
                return BadRequest(ModelState);
            }

            try
            {
                _logger.LogInformation("Starting updating log for tour: {Tour}", dto.tour_id);

                await _logService.UpdateLogAsync(logId, dto);
                return NoContent();

            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogError(ex, "Tour was not found when updating log");
                return StatusCode(404, new { message = "Tour not found" });  
            }
            catch (IndexOutOfRangeException ex)
            {
                _logger.LogError(ex, "Error when updating log");
                return StatusCode(400, new { message = ex });  
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while updating log");
                return StatusCode(500, new { message = "Internal Server Error" });  
            }
        }

    }
}