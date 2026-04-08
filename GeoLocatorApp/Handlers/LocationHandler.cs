using GeoLocatorApp.Interfaces;
using GeoLocatorApp.Models;

namespace GeoLocatorApp.Handlers
{
    public class LocationHandler
    {
        private readonly IGeocodingGateway _gateway;

        public LocationHandler(IGeocodingGateway gateway)
        {
            _gateway = gateway;
        }

        public async Task<List<LocationResult>> HandleAsync(string location)
        {
            return await _gateway.GetCoordinatesAsync(location);
        }
    }
}
