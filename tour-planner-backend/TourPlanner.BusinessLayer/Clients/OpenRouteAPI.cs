using System.Text.Json.Serialization;

namespace TourPlanner.BusinessLayer.Clients
{
    public class OrsRequest
    {
        public List<double[]> Coordinates {get; set;} = new();
    }

    public class OrsResponse
    {
        [JsonPropertyName("routes")]
        public List<OrsRoute> Routes { get; set; } = new();
    }

    public class OrsRoute
    {
        [JsonPropertyName("summary")]
        public OrsSummary Summary { get; set; } = new();

        [JsonPropertyName("geometry")]
        public string Geometry { get; set; } = string.Empty;
    }

    public class OrsSummary
    {
        [JsonPropertyName("distance")]
        public double Distance { get; set; }

        [JsonPropertyName("duration")]
        public double Duration { get; set; }
    }
}