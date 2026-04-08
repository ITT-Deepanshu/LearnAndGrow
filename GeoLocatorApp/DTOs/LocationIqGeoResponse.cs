using System.Text.Json.Serialization;

namespace GeoLocatorApp.DTOs
{
    public class LocationIqResult
    {
        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; }

        [JsonPropertyName("lat")]
        public string Lat { get; set; }

        [JsonPropertyName("lon")]
        public string Lon { get; set; }
    }
}
