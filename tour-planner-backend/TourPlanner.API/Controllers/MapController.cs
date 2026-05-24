using Microsoft.AspNetCore.Mvc;
using TourPlanner.BusinessLayer.Services;
using System.Net.Http.Json;

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

        [HttpPost("directions/{transportType}")]
        public async Task<IActionResult> GetDirections(string transportType, [FromBody] List<double[]> coordinates)
        {
            if (coordinates == null || coordinates.Count < 2) return BadRequest("At least 2 coordinates required");

            string? apiKey = Environment.GetEnvironmentVariable("OpenRoute_ApiKey");
            if (string.IsNullOrEmpty(apiKey)) return StatusCode(500, "API Key missing in backend");

            // Correct profile mapping if necessary, but we are sending strings like 'foot-walking' 
            // which ORS expects.
            var url = $"https://api.openrouteservice.org/v2/directions/{transportType}/geojson";
            
            try 
            {
                var requestBody = new { coordinates = coordinates };
                
                // Use a local request to avoid thread-safety issues with static client headers
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.TryAddWithoutValidation("Authorization", apiKey);
                request.Content = JsonContent.Create(requestBody);

                var response = await _httpClient.SendAsync(request);
                
                if (!response.IsSuccessStatusCode) 
                {
                    var error = await response.Content.ReadAsStringAsync();
                    return StatusCode((int)response.StatusCode, error);
                }
                
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
