using Shared.DTOs.Common;
using Shared.DTOs.DevicePreferences;
using System.Net.Http.Json;

namespace Web.Services;

public class DeviceApiClient(HttpClient httpClient)
{
    public async Task<ApiResult<PagedResult<DevicePreferenceDetailDto>>?> GetDevicesAsync(
        int page = 1,
        int pageSize = 50,
        string? search = null,
        CancellationToken ct = default)
    {
        try
        {
            var url = $"api/device-preference?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(search))
                url += $"&search={Uri.EscapeDataString(search)}";

            return await httpClient.GetFromJsonAsync<ApiResult<PagedResult<DevicePreferenceDetailDto>>>(url, ct);
        }
        catch (Exception) { return null; }
    }

    public async Task<ApiResult<bool>?> ResetDeviceAsync(string deviceId, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(
                $"api/device-preference/{Uri.EscapeDataString(deviceId)}/reset", new { }, ct);
            return await response.Content.ReadFromJsonAsync<ApiResult<bool>>(cancellationToken: ct);
        }
        catch (Exception) { return null; }
    }
}
