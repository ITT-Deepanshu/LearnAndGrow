using GeoLocatorApp.Models;

namespace GeoLocatorApp.Interfaces
{
    public interface IGeocodingGateway
    {
        Task<List<LocationResult>> GetCoordinatesAsync(string location);
    }
}
