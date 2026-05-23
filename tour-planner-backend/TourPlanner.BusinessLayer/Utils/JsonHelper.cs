using System.Text.Json;

namespace TourPlanner.BusinessLayer.Utils
{
    public static class JsonSerializerHelper
    {
        private static readonly JsonSerializerOptions _options = new()
        {
            WriteIndented = true
        };

        public static string SerializeList<T>(List<T> list)
        {
            return JsonSerializer.Serialize(list, _options);
        }
    }
}