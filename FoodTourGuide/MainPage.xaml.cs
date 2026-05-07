using FoodTourGuide.Data;
using FoodTourGuide.Models;
using Plugin.Maui.Audio;

namespace FoodTourGuide;

public partial class MainPage : ContentPage
{
    DatabaseService dbService = new DatabaseService();

    string lastPlayed = "";
    DateTime lastPlayedTime = DateTime.MinValue;

    public MainPage()
    {
        InitializeComponent();
        InitApp();
    }

    // 🚀 Khởi tạo app
    private async void InitApp()
    {
        await dbService.Init();

        var db = dbService.GetDb();

        // Xóa dữ liệu cũ (tránh trùng khi test)
        await db.DeleteAllAsync<PoI>();

        // Thêm dữ liệu mẫu
        await db.InsertAsync(new PoI
        {
            Name = "Bánh mì Vĩnh Thực",
            Latitude = 10.75,
            Longitude = 106.66,
            Radius = 0.2, // km (~200m)
            Priority = 1,
            Description = "Quán bánh mì nổi tiếng",
            AudioFile = "banhmi.mp3"
        });

        await db.InsertAsync(new PoI
        {
            Name = "Bún bò",
            Latitude = 10.76,
            Longitude = 106.67,
            Radius = 0.2,
            Priority = 2,
            Description = "Ăn là ghiền",
            AudioFile = "bunbo.mp3"
        });
        await db.InsertAsync(new PoI
        {
            Name = "Phở Hòa",
            Latitude = 10.73,
            Longitude = 106.68,
            Radius = 0.2,
            Priority = 3,
            Description = "Phở ngon nhất Sài Gòn",
            AudioFile = "phohoa.mp3"
        });
        await db.InsertAsync(new PoI
        {
            Name = "Cơm tấm Ba Ghiền",
            Latitude = 10.74,
            Longitude = 106.69,
            Radius = 0.2,
            Priority = 4,
            Description = "Cơm tấm ngon nhất Sài Gòn",
            AudioFile = "comtam.mp3"
        });
        await db.InsertAsync(new PoI
        {
            Name = "Hủ tiếu Nam Vang",
            Latitude = 10.77,
            Longitude = 106.65,
            Radius = 0.2,
            Priority = 5,
            Description = "Hủ tiếu ngon nhất Sài Gòn",
            AudioFile = "hutieu.mp3"
        });

        // Load danh sách lên UI
        var list = await db.Table<PoI>().ToListAsync();
        poiList.ItemsSource = list;

        // Lấy vị trí GPS
        await GetLocation();
    }

    // 📍 Lấy GPS
    private async Task GetLocation()
    {
        try
        {
            var request = new GeolocationRequest(GeolocationAccuracy.Medium);

            var location = await Geolocation.GetLocationAsync(request);

            if (location != null)
            {
                await FindNearestPoI(location.Latitude, location.Longitude);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi GPS", ex.Message, "OK");
        }
    }

    // 🧭 Tìm PoI gần nhất + auto phát audio
    private async Task FindNearestPoI(double userLat, double userLng)
    {
        var db = dbService.GetDb();
        var list = await db.Table<PoI>().ToListAsync();

        foreach (var poi in list)
        {
            var distance = CalculateDistance(userLat, userLng, poi.Latitude, poi.Longitude);

            // Nếu vào vùng
            if (distance <= poi.Radius)
            {
                // 🔥 chống spam (30s)
                if (poi.Name == lastPlayed &&
                    (DateTime.Now - lastPlayedTime).TotalSeconds < 30)
                {
                    return;
                }

                lastPlayed = poi.Name;
                lastPlayedTime = DateTime.Now;

                await DisplayAlert("Đang phát", poi.Name, "OK");

                await PlayAudio(poi.AudioFile);

                break;
            }
        }
    }

    // 📏 Tính khoảng cách (Haversine)
    double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        var R = 6371; // km

        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180) *
                Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLon / 2) *
                Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return R * c;
    }

    // 🔊 Phát audio
    private async Task PlayAudio(string fileName)
    {
        try
        {
            var player = AudioManager.Current.CreatePlayer(
                await FileSystem.OpenAppPackageFileAsync(fileName));

            player.Play();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi Audio", ex.Message, "OK");
        }
    }
}