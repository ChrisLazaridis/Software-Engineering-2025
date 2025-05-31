using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace SoftEng2025.Services
{
    public class GeocodingService : IGeocodingService
    {
        private readonly HttpClient _http;
        private readonly IMemoryCache _cache;

        public GeocodingService(HttpClient http, IMemoryCache cache)
        {
            _http = http;
            _cache = cache;
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("SoftEng2025App/1.0");
        }

        public Task<string> GetAddressAsync(double lat, double lon)
            => _cache.GetOrCreateAsync($"{lat},{lon}", async entry =>
            {
                // Cache for 1 day
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1);

                try
                {
                    var url = $"https://nominatim.openstreetmap.org/reverse?format=jsonv2&addressdetails=1&lat={lat}&lon={lon}";
                    var json = await _http.GetStringAsync(url);
                    using var doc = JsonDocument.Parse(json);

                    if (!doc.RootElement.TryGetProperty("address", out var addr))
                        // If JSON doesn’t contain "address", fall back to coordinates
                        return $"{lat}, {lon}";

                    // 1) house_number
                    string house = "";
                    if (addr.TryGetProperty("house_number", out var hn) && hn.ValueKind == JsonValueKind.String)
                        house = hn.GetString();

                    // 2) street-like
                    string street = "";
                    foreach (var key in new[] { "road", "street", "pedestrian", "residential" })
                    {
                        if (addr.TryGetProperty(key, out var el) && el.ValueKind == JsonValueKind.String)
                        {
                            street = el.GetString();
                            break;
                        }
                    }

                    // 3) postcode
                    string postcode = "";
                    if (addr.TryGetProperty("postcode", out var pc) && pc.ValueKind == JsonValueKind.String)
                        postcode = pc.GetString();

                    // 4) city/town/village
                    string city = "";
                    foreach (var key in new[] { "city", "town", "village", "municipality" })
                    {
                        if (addr.TryGetProperty(key, out var el) && el.ValueKind == JsonValueKind.String)
                        {
                            city = el.GetString();
                            break;
                        }
                    }

                    // combine house + street
                    string streetPart;
                    if (!string.IsNullOrWhiteSpace(house) && !string.IsNullOrWhiteSpace(street))
                        streetPart = $"{house} {street}";
                    else if (!string.IsNullOrWhiteSpace(street))
                        streetPart = street;
                    else if (!string.IsNullOrWhiteSpace(house))
                        streetPart = house;
                    else
                        streetPart = "";

                    var parts = new List<string>();
                    if (!string.IsNullOrWhiteSpace(streetPart)) parts.Add(streetPart);
                    if (!string.IsNullOrWhiteSpace(postcode)) parts.Add(postcode);
                    if (!string.IsNullOrWhiteSpace(city)) parts.Add(city);

                    // If we built at least one part, join them; otherwise fallback to coordinates
                    return parts.Count > 0
                        ? string.Join(", ", parts)
                        : $"{lat}, {lon}";
                }
                catch
                {
                    // On any exception (network, JSON parse, etc.), just return "lat, lon"
                    return $"{lat}, {lon}";
                }
            });

        public Task<string[]> GetAddressesAsync(IEnumerable<(double Lat, double Lon)> coords)
        {
            // Kick off all lookups concurrently; each one has its own try/catch above
            var tasks = coords.Select(c => GetAddressAsync(c.Lat, c.Lon));
            return Task.WhenAll(tasks);
        }
    }
}
