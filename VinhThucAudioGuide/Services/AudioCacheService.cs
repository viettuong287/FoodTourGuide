using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace VinhThucAudioGuide.Services
{
    public static class AudioCacheService
    {
        // Returns local file path if cached or downloaded successfully; null on failure
        public static async Task<string> GetOrFetchAudioAsync(string stallId, string langCode)
        {
            try
            {
                if (string.IsNullOrEmpty(stallId)) return null;
                var cacheDir = FileSystem.CacheDirectory;
                var safeId = stallId.Replace("-", "");
                var fileName = $"tts_{safeId}_{langCode}.mp3";
                var filePath = Path.Combine(cacheDir, fileName);

                if (File.Exists(filePath)) return filePath;

                var apiBase = Preferences.Default.Get("RemoteApiBase", string.Empty);
                if (string.IsNullOrWhiteSpace(apiBase)) return null;

                var url = apiBase.TrimEnd('/') + $"/api/mobile/tts?stallId={stallId}&lang={langCode}";

                using var client = new HttpClient();
                var resp = await client.GetAsync(url);
                if (!resp.IsSuccessStatusCode) return null;

                var bytes = await resp.Content.ReadAsByteArrayAsync();
                if (bytes == null || bytes.Length == 0) return null;

                await File.WriteAllBytesAsync(filePath, bytes);
                return filePath;
            }
            catch
            {
                return null;
            }
        }
    }
}
