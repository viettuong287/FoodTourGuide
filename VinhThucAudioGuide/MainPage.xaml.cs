using Mapsui;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Providers;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;           // Trị lỗi ContentPage, Button, SelectionChangedEventArgs
using Microsoft.Maui.Devices.Sensors;    // Trị lỗi Location, Geolocation, GeolocationRequest
using Microsoft.Maui.Graphics;           // Trị lỗi Colors
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;            // Dùng để đọc Preferences (Sổ tay cài đặt ngôn ngữ)
using Plugin.Maui.Audio;
using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using VinhThucAudioGuide.Services;
using Color = Microsoft.Maui.Graphics.Color;

namespace VinhThucAudioGuide
{
    public class POI
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string ImageUrl { get; set; }
        public double Rating { get; set; }
        public Color PinColor { get; set; } = Colors.Red;
        public Dictionary<string, string> AudioScripts { get; set; } = new Dictionary<string, string>();
    }

    public partial class MainPage : ContentPage
    {
        private List<POI> _allPois = new List<POI>();
        private IAudioPlayer _currentAudioPlayer;
        private Location _currentUserLocation;
        private MemoryLayer _routeLayer;
        private int _speechId = 0;
        private POI _selectedPoiForAudio = null;

        //API key
        private readonly string ORS_API_KEY = "eyJvcmciOiI1YjNjZTM1OTc4NTExMTAwMDFjZjYyNDgiLCJpZCI6IjM4MTUxY2VmYzJmZDQ1ZDdhMGQ1NjYyN2ViMzlhZTNjIiwiaCI6Im11cm11cjY0In0=";

        public MainPage()
        {
            InitializeComponent();
            InitMap();
            GetLocationAndCenter();
        }
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadDataAsync(); // Nạp dữ liệu từ DB (Có await đàng hoàng)
        }

        private void InitMap()
        {
            var map = new Mapsui.Map();
            map.Layers.Add(OpenStreetMap.CreateTileLayer());
            map.Widgets.Clear();
            mapView.Map = map;

            _routeLayer = new MemoryLayer { Name = "Route" };
            mapView.Map.Layers.Add(_routeLayer);

            var (x, y) = SphericalMercator.FromLonLat(106.6923, 10.7716);
            mapView.Map.Navigator.CenterOnAndZoomTo(new MPoint(x, y), 2.0);
        }

        private async Task LoadDataAsync()
        {
            // 1. Khởi tạo danh sách trống
            _allPois = new List<POI>();

            // 2. Gọi bộ não Database lên
            var dbService = new VinhThucAudioGuide.Services.LocalDbService();
            var danhSachTuDB = await dbService.GetAllTourLocations();

            // 3. Quét qua DB và nặn ra các điểm POI
            foreach (var loc in danhSachTuDB)
            {
                var poiMoi = new POI
                {
                    Id = loc.Id,
                    Name = loc.LocationName,
                    Category = loc.Category,
                    ImageUrl = loc.ImageUrl,
                    Latitude = loc.Latitude,
                    Longitude = loc.Longitude
                };
                _allPois.Add(poiMoi);
            }

            // 4. Bơm dữ liệu ra UI (Bắt buộc phải nằm trong MainThread)
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Dọn sạch ghim trên bản đồ (để trống trơn chờ khách bấm list)
                mapView.Pins.Clear();
                mapView.Refresh();

                // Bơm 5 địa điểm ra cái danh sách cvPoiList có sẵn của sếp!
                cvPoiList.ItemsSource = _allPois;
            });
        }



        private async void GetLocationAndCenter()
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                    if (status != PermissionStatus.Granted)
                    {
                        await DisplayAlert("Thiếu quyền", "Bro phải cho phép app dùng vị trí thì mới lấy được GPS nhé!", "OK");
                        return;
                    }
                }

                var request = new GeolocationRequest(GeolocationAccuracy.Medium);
                var location = await Geolocation.Default.GetLocationAsync(request);

                if (location != null)
                {
                    _currentUserLocation = location;
                    mapView.MyLocationLayer.UpdateMyLocation(new Mapsui.UI.Maui.Position(location.Latitude, location.Longitude));
                    var (x, y) = SphericalMercator.FromLonLat(location.Longitude, location.Latitude);
                    mapView.Map.Navigator.CenterOnAndZoomTo(new MPoint(x, y), 2.0);
                    mapView.Refresh();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi GPS", "Không lấy được vị trí. Nhớ bật GPS (Vị trí) trên điện thoại nha bro!\n" + ex.Message, "OK");
            }
        }

        private void MyLocation_Clicked(object sender, EventArgs e)
        {
            GetLocationAndCenter();
        }

        private void Category_Clicked(object sender, EventArgs e)
        {
            var btn = sender as Button;
            // Chuẩn hoá nhãn nút thành 3 category chuẩn của app
            var text = (btn?.Text ?? string.Empty).ToLowerInvariant();
            string category;
            if (text.Contains("thức") || text.Contains("ẩm") || text.Contains("🍴")) category = "Thức ăn";
            else if (text.Contains("vui") || text.Contains("du lịch") || text.Contains("🏛")) category = "Vui chơi";
            else if (text.Contains("lễ") || text.Contains("sự kiện") || text.Contains("🎉")) category = "Lễ hội";
            else category = string.Empty;

            if (string.IsNullOrEmpty(category))
            {
                // nếu không nhận diện được, hiện tất cả
                cvPoiList.ItemsSource = _allPois;
            }
            else
            {
                cvPoiList.ItemsSource = _allPois.Where(p => (p.Category ?? string.Empty) == category).ToList();
            }
        }

        private async void Poi_Selected(object sender, SelectionChangedEventArgs e)
        {
            var target = e.CurrentSelection.FirstOrDefault() as POI;
            if (target == null) return;

            _selectedPoiForAudio = target;
            cvPoiList.SelectedItem = null;

            mapView.Pins.Clear();
            mapView.Pins.Add(new Mapsui.UI.Maui.Pin(mapView)
            {
                Position = new Mapsui.UI.Maui.Position(target.Latitude, target.Longitude),
                Type = Mapsui.UI.Maui.PinType.Pin,
                Color = target.PinColor,
                Label = target.Name
            });

            var (x, y) = SphericalMercator.FromLonLat(target.Longitude, target.Latitude);
            mapView.Map.Navigator.CenterOnAndZoomTo(new MPoint(x, y), 2.0);

            if (ORS_API_KEY == "ĐIỀN_API_KEY_CỦA_BRO_VÀO_ĐÂY")
            {
                await DisplayAlert("Lỗi", "Bro chưa dán API Key vào dòng 37 kìa!", "OK");
                return;
            }

            if (_currentUserLocation == null)
            {
                await DrawRouteAsync(106.6983, 10.7719, target.Longitude, target.Latitude);
            }
            else
            {
                await DrawRouteAsync(_currentUserLocation.Longitude, _currentUserLocation.Latitude, target.Longitude, target.Latitude);
            }
        }

        private async Task DrawRouteAsync(double startLon, double startLat, double endLon, double endLat) //vẽ đường đi
        {
            try
            {
                string url = $"https://api.openrouteservice.org/v2/directions/driving-car?api_key={ORS_API_KEY}&start={startLon.ToString(System.Globalization.CultureInfo.InvariantCulture)},{startLat.ToString(System.Globalization.CultureInfo.InvariantCulture)}&end={endLon.ToString(System.Globalization.CultureInfo.InvariantCulture)},{endLat.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

                using var client = new HttpClient();
                var response = await client.GetStringAsync(url);
                var doc = JsonDocument.Parse(response);
                var coordinates = doc.RootElement.GetProperty("features")[0].GetProperty("geometry").GetProperty("coordinates");

                var points = new List<NetTopologySuite.Geometries.Coordinate>();
                foreach (var coord in coordinates.EnumerateArray())
                {
                    var (x, y) = SphericalMercator.FromLonLat(coord[0].GetDouble(), coord[1].GetDouble());
                    points.Add(new NetTopologySuite.Geometries.Coordinate(x, y));
                }

                var lineString = new NetTopologySuite.Geometries.LineString(points.ToArray());
                _routeLayer.Features = new List<IFeature> {
                    new Mapsui.Nts.GeometryFeature {
                        Geometry = lineString,
                        Styles = new[] { new VectorStyle { Line = new Pen(Mapsui.Styles.Color.Blue, 5) } }
                    }
                };
                _routeLayer.DataHasChanged();
                mapView.Refresh();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi chỉ đường", "Không vẽ được đường. Kiểm tra lại API Key hoặc Mạng nhé!\nChi tiết: " + ex.Message, "OK");
            }
        }

        private async void Speak_Clicked(object sender, EventArgs e) //thuyết minh
        {
            if (_selectedPoiForAudio == null) { await DisplayAlert("Thông báo", "Chọn quán ăn trong danh sách trước nhé!", "OK"); return; }

            var selectedPoi = _selectedPoiForAudio;

            // Hiển thị 5 ngôn ngữ chuẩn: Việt, Anh, Pháp, Trung, Hàn
            string action = await DisplayActionSheet("Ngôn ngữ", "Hủy", null,
                "🇻🇳 Tiếng Việt",
                "🇬🇧 English",
                "🇫🇷 Français",
                "🇨🇳 中文",
                "🇰🇷 한국어");
            if (action == "Hủy" || string.IsNullOrEmpty(action)) return;

            string text = "", lang = "vi";
            if (action.Contains("Việt")) { text = selectedPoi.AudioScripts.GetValueOrDefault("vi", selectedPoi.Name); lang = "vi"; }
            else if (action.Contains("English")) { text = selectedPoi.AudioScripts.GetValueOrDefault("en", selectedPoi.Name); lang = "en"; }
            else if (action.Contains("Français")) { text = selectedPoi.AudioScripts.GetValueOrDefault("fr", selectedPoi.Name); lang = "fr"; }
            else if (action.Contains("中文")) { text = selectedPoi.AudioScripts.GetValueOrDefault("zh", selectedPoi.Name); lang = "zh"; }
            else if (action.Contains("한국어")) { text = selectedPoi.AudioScripts.GetValueOrDefault("ko", selectedPoi.Name); lang = "ko"; }

            _speechId++;
            int currentId = _speechId;
            _currentAudioPlayer?.Stop();

         
            var finalSentences = new List<string>();
            var rawParts = text.Split(new[] { '.', '\n', '?', '!' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in rawParts)
            {
                var words = part.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                string chunk = "";
                foreach (var word in words)
                {
                    
                    if (chunk.Length + word.Length > 150)
                    {
                        finalSentences.Add(chunk.Trim());
                        chunk = "";
                    }
                    chunk += word + " ";
                }
                if (!string.IsNullOrWhiteSpace(chunk)) finalSentences.Add(chunk.Trim());
            }

            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

                // Try to fetch cached cloud TTS file for this POI
                string cachedPath = await Services.AudioCacheService.GetOrFetchAudioAsync(_selectedPoiForAudio.Id, lang);
                if (!string.IsNullOrEmpty(cachedPath) && File.Exists(cachedPath))
                {
                    // Play cached file
                    _currentAudioPlayer?.Stop();
                    _currentAudioPlayer = AudioManager.Current.CreatePlayer(cachedPath);
                    _currentAudioPlayer.Play();
                    return;
                }

                // If no cached file, try to stream/generate per-sentence and cache whole file when possible
                foreach (var sentence in finalSentences)
                {
                    if (currentId != _speechId) break;

                    string cleanSentence = sentence.Trim();
                    if (string.IsNullOrEmpty(cleanSentence)) continue;

                    // Try to download full TTS for single sentence from cloud (server may support chunking)
                    string ttsUrl = Preferences.Default.Get("RemoteApiBase", string.Empty);
                    byte[] audioBytes = null;
                    if (!string.IsNullOrWhiteSpace(ttsUrl))
                    {
                        try
                        {
                            var api = ttsUrl.TrimEnd('/') + $"/api/mobile/tts?locationId={_selectedPoiForAudio.Id}&lang={lang}&text=" + Uri.EscapeDataString(cleanSentence);
                            var resp = await client.GetAsync(api);
                            if (resp.IsSuccessStatusCode)
                            {
                                audioBytes = await resp.Content.ReadAsByteArrayAsync();
                            }
                        }
                        catch { audioBytes = null; }
                    }

                    // Fallback to Google TTS (not ideal for production, but works)
                    if (audioBytes == null || audioBytes.Length == 0)
                    {
                        var url = $"https://translate.google.com/translate_tts?ie=UTF-8&q={Uri.EscapeDataString(cleanSentence)}&tl={lang}&client=tw-ob";
                        audioBytes = await client.GetByteArrayAsync(url);
                    }

                    if (currentId != _speechId) break;

                    _currentAudioPlayer?.Stop();
                    _currentAudioPlayer = AudioManager.Current.CreatePlayer(new MemoryStream(audioBytes));
                    _currentAudioPlayer.Play();

                    await Task.Delay(300);

                    while (_currentAudioPlayer.IsPlaying && currentId == _speechId)
                    {
                        await Task.Delay(100);
                    }
                }

                // After successful stream, try to fetch and cache full audio for POI (best-effort)
                try
                {
                    var apiBase = Preferences.Default.Get("RemoteApiBase", string.Empty);
                    if (!string.IsNullOrWhiteSpace(apiBase))
                    {
                        var cachePath = await Services.AudioCacheService.GetOrFetchAudioAsync(_selectedPoiForAudio.Id, lang);
                        // ignore result; cache method already writes file
                    }
                }
                catch { }
            }
            catch (Exception ex)
            {
                // No network / cloud failed -> fallback to native TTS via platform service
                try
                {
                    var tts = Microsoft.Maui.Controls.DependencyService.Get<VinhThucAudioGuide.Services.IPlatformTts>();
                    string locale = lang switch
                    {
                        "vi" => "vi-VN",
                        "en" => "en-US",
                        "fr" => "fr-FR",
                        "zh" => "zh-CN",
                        "ko" => "ko-KR",
                        _ => "en-US"
                    };

                    if (tts != null)
                    {
                        tts.Speak(text, locale);
                    }
                    else
                    {
                        await DisplayAlert("Lỗi âm thanh", "Không có dịch vụ TTS trên thiết bị.", "OK");
                    }
                }
                catch
                {
                    await DisplayAlert("Lỗi âm thanh", "Không thể phát âm thanh cả cloud lẫn native.", "OK");
                }
            }
        }

        private void StopSpeech_Clicked(object sender, EventArgs e)
        {
            _speechId++;
            if (_currentAudioPlayer != null && _currentAudioPlayer.IsPlaying) _currentAudioPlayer.Stop();
        }
        // ==========================================
        // CÁC HÀM LỌC DANH MỤC
        // ==========================================

        private void Filter_All_Clicked(object sender, EventArgs e)
        {
            // Hiện tất cả
            cvPoiList.ItemsSource = _allPois;
        }

        private void Filter_Food_Clicked(object sender, EventArgs e)
        {
            // Lọc Thức ăn (Sườn bì chưởng, Bánh mì Huỳnh Hoa)
            cvPoiList.ItemsSource = _allPois.Where(x => x.Category == "Thức ăn").ToList();
        }

        private void Filter_Travel_Clicked(object sender, EventArgs e)
        {
            // Lọc Vui chơi (Công viên Sáng Tạo, Tao Đàn)
            cvPoiList.ItemsSource = _allPois.Where(x => x.Category == "Vui chơi").ToList();
        }

        private void Filter_Event_Clicked(object sender, EventArgs e)
        {
            // Lọc Lễ hội (Phố Lồng Đèn)
            cvPoiList.ItemsSource = _allPois.Where(x => x.Category == "Lễ hội").ToList();
        }   
    }

}
