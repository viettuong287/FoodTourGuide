using Shared.DTOs.Common;
using Shared.DTOs.Geo;
using System.Net.Http.Json;

namespace Web.Services;

public class GeoApiClient(HttpClient httpClient)
{
    public async Task<ApiResult<ActiveDevicesSummaryDto>?> GetActiveDevicesAsync(
        int withinSeconds = 30,
        CancellationToken ct = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<ApiResult<ActiveDevicesSummaryDto>>(
                $"api/geo/active-devices?withinSeconds={withinSeconds}", ct);
        }
        catch (HttpRequestException) { return null; }
    }

    public async Task<ApiResult<bool>?> ResetDeviceAsync(string deviceId, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(
                $"api/device-preference/{Uri.EscapeDataString(deviceId)}/reset", new { }, ct);
            return await response.Content.ReadFromJsonAsync<ApiResult<bool>>(cancellationToken: ct);
        }
        catch (HttpRequestException) { return null; }
    }
}
