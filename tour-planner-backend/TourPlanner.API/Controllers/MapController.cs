using Microsoft.AspNetCore.Mvc;
using TourPlanner.BusinessLayer.Services;

namespace TourPlanner.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MapController : ControllerBase
    {
        private readonly ITourService _tourService;
        private static readonly HttpClient _httpClient = new();

        public MapController(ITourService tourService)
        {
            _tourService = tourService;
        }

        [HttpGet("geocode")]
        public async Task<IActionResult> Geocode([FromQuery] string text)
        {
            if (string.IsNullOrEmpty(text)) return BadRequest("Text is required");

            string? apiKey = Environment.GetEnvironmentVariable("OpenRoute_ApiKey");
            if (string.IsNullOrEmpty(apiKey)) return StatusCode(500, "API Key missing in backend");

            var url = $"https://api.openrouteservice.org/geocode/search?api_key={apiKey}&text={Uri.EscapeDataString(text)}&size=5";
            
            try 
            {
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return StatusCode((int)response.StatusCode);
                
                var content = await response.Content.ReadAsStringAsync();
                return Content(content, "application/json");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
