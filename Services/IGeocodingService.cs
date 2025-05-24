// Services/IGeocodingService.cs
using System.Threading.Tasks;

namespace SoftEng2025.Services
{
    public interface IGeocodingService
    {
        Task<string> GetAddressAsync(double lat, double lon);
        Task<string[]> GetAddressesAsync(IEnumerable<(double Lat, double Lon)> coords);
    }
}