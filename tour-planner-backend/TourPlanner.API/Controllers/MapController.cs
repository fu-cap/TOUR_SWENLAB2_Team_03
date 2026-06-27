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
        private readonly ILogger<MapController> _logger;
        private static readonly HttpClient _httpClient = new();

        public MapController(ITourService tourService, ILogger<MapController> logger)
        {
            _tourService = tourService;
            _logger = logger;
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
                _logger.LogError(ex, "Error while calling geocode API for text: {Text}", text);
                return StatusCode(500, new { message = "Geocoding service unavailable" });
            }
        }

        [HttpPost("directions/{transportType}")]
        public async Task<IActionResult> GetDirections(string transportType, [FromBody] List<double[]> coordinates)
        {
            if (coordinates == null || coordinates.Count < 2) return BadRequest("At least 2 coordinates required");

            if (coordinates.Any(c => c == null || c.Length < 2))
                return BadRequest("Each coordinate must contain at least [longitude, latitude]");

            string? apiKey = Environment.GetEnvironmentVariable("OpenRoute_ApiKey");
            if (string.IsNullOrEmpty(apiKey)) return StatusCode(500, "API Key missing in backend");

            var url = $"https://api.openrouteservice.org/v2/directions/{transportType}/geojson";

            try
            {
                var requestBody = new { coordinates = coordinates };

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
                _logger.LogError(ex, "Error while calling directions API for transport type: {TransportType}", transportType);
                return StatusCode(500, new { message = "Route service unavailable" });
            }
        }
    }
}
