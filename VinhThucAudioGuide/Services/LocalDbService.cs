using SQLite;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using VinhThucAudioGuide.Models;

namespace VinhThucAudioGuide.Services;

public class LocalDbService
{
    private SQLiteAsyncConnection _db;

    public async Task Init()
    {
        if (_db != null) return;

        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "VinhThucData.db");
        _db = new SQLiteAsyncConnection(dbPath);

        await _db.CreateTableAsync<TourLocation>();
        await _db.CreateTableAsync<Language>();
        await _db.CreateTableAsync<Script>();
        await _db.CreateTableAsync<QRCodeData>();
        await _db.CreateTableAsync<UserDevice>();

        // await SeedData(); // Tắt dữ liệu mẫu để chỉ hiển thị dữ liệu thực tế từ Web CMS
    }

    public async Task ClearAllDataAsync()
    {
        await Init();
        await _db.DeleteAllAsync<TourLocation>();
        await _db.DeleteAllAsync<Language>();
        await _db.DeleteAllAsync<Script>();
        await _db.DeleteAllAsync<QRCodeData>();
    }

    public async Task<Dictionary<string, string>> GetScriptsForLocation(int locationId)
    {
        await Init();
        var result = new Dictionary<string, string>();

        var scripts = await _db.Table<Script>().Where(s => s.LocationId == locationId).ToListAsync();
        foreach (var s in scripts)
        {
            var lang = await _db.Table<Language>().Where(l => l.Id == s.LanguageId).FirstOrDefaultAsync();
            if (lang != null && !string.IsNullOrEmpty(lang.LangCode))
            {
                result[lang.LangCode] = string.IsNullOrWhiteSpace(s.Content) ? s.Title ?? string.Empty : s.Content;
            }
        }

        return result;
    }

    private async Task SeedData()
    {
        var vi = await EnsureLanguage("vi", "Tiếng Việt");
        var en = await EnsureLanguage("en", "English");
        var fr = await EnsureLanguage("fr", "Français");
        var zh = await EnsureLanguage("zh", "中文");
        var ko = await EnsureLanguage("ko", "한국어");

        var allLocations = PoiData.GetNewLocations();

        var scriptsMap = PoiData.GetScripts();

        foreach (var (name, cat, img, lat, lon) in allLocations)
        {
            var loc = await _db.Table<TourLocation>().Where(t => t.LocationName == name).FirstOrDefaultAsync();
            if (loc == null)
            {
                loc = new TourLocation { LocationName = name, Address = cat, ImageUrl = img, Latitude = lat, Longitude = lon };
                await _db.InsertAsync(loc);

                string qrCode = "QR_" + name.Replace(" ", "").ToUpper();
                await _db.InsertAsync(new QRCodeData { CodeValue = qrCode, LocationId = loc.Id });
            }
            else
            {
                // Cập nhật lại hình ảnh phòng trường hợp link cũ bị hỏng
                loc.ImageUrl = img;
                await _db.UpdateAsync(loc);
            }

            if (scriptsMap.ContainsKey(name))
            {
                var s = scriptsMap[name];
                // Xóa kịch bản cũ để chèn lại kịch bản mới (tránh trùng lặp nếu đã có)
                await _db.Table<Script>().Where(script => script.LocationId == loc.Id).DeleteAsync();
                
                await _db.InsertAsync(new Script { LocationId = loc.Id, LanguageId = vi.Id, Title = name, Content = s.vi });
                await _db.InsertAsync(new Script { LocationId = loc.Id, LanguageId = en.Id, Title = name, Content = s.en });
                await _db.InsertAsync(new Script { LocationId = loc.Id, LanguageId = fr.Id, Title = name, Content = s.fr });
                await _db.InsertAsync(new Script { LocationId = loc.Id, LanguageId = zh.Id, Title = name, Content = s.zh });
                await _db.InsertAsync(new Script { LocationId = loc.Id, LanguageId = ko.Id, Title = name, Content = s.ko });
            }
        }
    }

    private async Task<Language> EnsureLanguage(string code, string name)
    {
        var existing = await _db.Table<Language>().Where(l => l.LangCode == code).FirstOrDefaultAsync();
        if (existing != null) return existing;
        var lang = new Language { LangCode = code, LangName = name };
        await _db.InsertAsync(lang);
        return lang;
    }

    public async Task<Script> GetScriptByQRAndLanguage(string scannedCode, string langCode)
    {
        await Init();

        var qr = await _db.Table<QRCodeData>().Where(q => q.CodeValue == scannedCode).FirstOrDefaultAsync();
        if (qr == null) return null;

        var lang = await _db.Table<Language>().Where(l => l.LangCode == langCode).FirstOrDefaultAsync();
        if (lang == null) return null;

        var script = await _db.Table<Script>()
            .Where(s => s.LocationId == qr.LocationId && s.LanguageId == lang.Id)
            .FirstOrDefaultAsync();

        return script;
    }

    public async Task<TourLocation> GetTourLocationByServerId(string serverId)
    {
        await Init();
        return await _db.Table<TourLocation>().Where(t => t.ServerId == serverId).FirstOrDefaultAsync();
    }

    public async Task<List<TourLocation>> GetAllTourLocations()
    {
        await Init();
        return await _db.Table<TourLocation>().ToListAsync();
    }

    public async Task<List<Language>> GetAllLanguages()
    {
        await Init();
        return await _db.Table<Language>().ToListAsync();
    }

    public async Task<int> UpsertTourLocations(List<TourLocation> remoteList)
    {
        await Init();
        int added = 0;

        foreach (var r in remoteList)
        {
            var existing = await _db.Table<TourLocation>()
                .Where(t => t.LocationName == r.LocationName)
                .FirstOrDefaultAsync();

            if (existing == null)
            {
                await _db.InsertAsync(r);
                added++;
            }
            else
            {
                bool dirty = false;
                if (existing.ImageUrl != r.ImageUrl) { existing.ImageUrl = r.ImageUrl; dirty = true; }
                if (existing.Latitude != r.Latitude) { existing.Latitude = r.Latitude; dirty = true; }
                if (existing.Longitude != r.Longitude) { existing.Longitude = r.Longitude; dirty = true; }
                if (existing.Address != r.Address) { existing.Address = r.Address; dirty = true; }
                if (dirty) await _db.UpdateAsync(existing);
            }
        }

        return added;
    }

    /// <summary>
    /// Đồng bộ dữ liệu từ server xuống local SQLite.
    /// Tham số progress (tuỳ chọn) dùng để báo cáo tiến độ cho UI SplashPage.
    /// Giá trị Percent nằm trong [0, 1] thể hiện tiến độ của riêng quá trình sync này.
    /// </summary>
    public async Task SyncWithServerAsync(ApiService apiService, IProgress<(double Percent, string Status)>? progress = null)
    {
        await Init();

        progress?.Report((0.05, "Đang tải dữ liệu từ máy chủ..."));

        // Lấy thời gian đồng bộ thành công gần nhất
        var lastSyncStr = Preferences.Get("LastSyncTime", string.Empty);
        DateTimeOffset? lastSync = null;
        if (!string.IsNullOrEmpty(lastSyncStr) && DateTimeOffset.TryParse(lastSyncStr, out var parsedDate))
        {
            lastSync = parsedDate;
        }

        var data = await apiService.GetSyncDataAsync(lastSync);
        if (data == null)
        {
            // Offline hoặc lỗi → coi như đã xong, dùng dữ liệu cũ
            progress?.Report((1.0, "Sử dụng dữ liệu đã lưu"));
            return;
        }

        progress?.Report((0.20, $"Đang đồng bộ {data.Languages.Count} ngôn ngữ..."));

        // 1. Sync Languages - bao gồm FlagCode để mobile hiển thị cờ quốc gia
        // khi user chọn ngôn ngữ thuyết minh trên MainPage.
        foreach (var langData in data.Languages)
        {
            var existing = await _db.Table<Language>().Where(l => l.ServerId == langData.ServerId || l.LangCode == langData.Code).FirstOrDefaultAsync();
            if (existing == null)
            {
                await _db.InsertAsync(new Language
                {
                    ServerId = langData.ServerId,
                    LangCode = langData.Code,
                    LangName = langData.Name,
                    FlagCode = langData.FlagCode ?? string.Empty
                });
            }
            else
            {
                existing.ServerId = langData.ServerId;
                existing.LangName = langData.Name;
                if (!string.IsNullOrEmpty(langData.FlagCode))
                    existing.FlagCode = langData.FlagCode;
                await _db.UpdateAsync(existing);
            }
        }

        progress?.Report((0.40, $"Đang đồng bộ {data.Locations.Count} POI..."));

        // 2. Sync Locations
        int locIndex = 0;
        int locTotal = Math.Max(1, data.Locations.Count);
        foreach (var locData in data.Locations)
        {
            var existing = await _db.Table<TourLocation>().Where(t => t.ServerId == locData.ServerId || t.LocationName == locData.Name).FirstOrDefaultAsync();
            if (existing == null)
            {
                // Nếu địa điểm mới đã bị xóa trên server thì không cần insert
                if (!locData.IsActive) continue;

                var newLoc = new TourLocation
                {
                    ServerId = locData.ServerId,
                    LocationName = locData.Name,
                    Address = locData.Address,
                    ImageUrl = locData.ImageUrl,
                    Latitude = locData.Latitude,
                    Longitude = locData.Longitude,
                    IsActive = locData.IsActive
                };
                await _db.InsertAsync(newLoc);
                
                // Tự động tạo QR Code cho location mới
                string qrCode = "QR_" + locData.Name.Replace(" ", "").ToUpper();
                await _db.InsertAsync(new QRCodeData { CodeValue = qrCode, LocationId = newLoc.Id });
            }
            else
            {
                // Cập nhật toàn bộ thông tin mới nhất
                existing.ServerId = locData.ServerId;
                existing.LocationName = locData.Name;
                existing.Address = locData.Address;
                existing.ImageUrl = locData.ImageUrl;
                existing.Latitude = locData.Latitude;
                existing.Longitude = locData.Longitude;
                existing.IsActive = locData.IsActive;
                await _db.UpdateAsync(existing);
            }

            // Báo cáo tiến độ cho từng location đã xử lý.
            // Map vùng [0.40, 0.70] cho phase Locations.
            locIndex++;
            var locPct = 0.40 + 0.30 * ((double)locIndex / locTotal);
            progress?.Report((locPct, $"Đang nạp POI {locIndex}/{locTotal}..."));
        }

        progress?.Report((0.70, $"Đang đồng bộ {data.Scripts.Count} kịch bản..."));

        // 3. Sync Scripts
        int sIndex = 0;
        int sTotal = Math.Max(1, data.Scripts.Count);
        foreach (var scriptData in data.Scripts)
        {
            var loc = await _db.Table<TourLocation>().Where(t => t.ServerId == scriptData.LocationId).FirstOrDefaultAsync();
            var lang = await _db.Table<Language>().Where(l => l.ServerId == scriptData.LanguageId).FirstOrDefaultAsync();
            
            if (loc == null || lang == null) continue;

            var existing = await _db.Table<Script>().Where(s => s.ServerId == scriptData.ServerId || (s.LocationId == loc.Id && s.LanguageId == lang.Id)).FirstOrDefaultAsync();
            if (existing == null)
            {
                await _db.InsertAsync(new Script
                {
                    ServerId = scriptData.ServerId,
                    LocationId = loc.Id,
                    LanguageId = lang.Id,
                    Title = scriptData.Title,
                    Content = scriptData.Content
                });
            }
            else
            {
                existing.ServerId = scriptData.ServerId;
                existing.Title = scriptData.Title;
                existing.Content = scriptData.Content;
                await _db.UpdateAsync(existing);
            }

            // Báo cáo tiến độ cho mỗi script đã xử lý — map vùng [0.70, 0.95].
            sIndex++;
            var sPct = 0.70 + 0.25 * ((double)sIndex / sTotal);
            progress?.Report((sPct, $"Đang nạp kịch bản {sIndex}/{sTotal}..."));
        }

        // Lưu lại thời gian đã đồng bộ thành công
        Preferences.Set("LastSyncTime", DateTimeOffset.UtcNow.ToString("O"));
        progress?.Report((1.0, "Hoàn tất đồng bộ"));
    }
}