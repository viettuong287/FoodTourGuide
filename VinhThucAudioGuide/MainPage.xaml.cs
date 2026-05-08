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
using System.ComponentModel;
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
        public string ServerId { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
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
        private string _activeCategoryFilter = string.Empty;

        //API key
        private readonly string ORS_API_KEY = "eyJvcmciOiI1YjNjZTM1OTc4NTExMTAwMDFjZjYyNDgiLCJpZCI6IjM4MTUxY2VmYzJmZDQ1ZDdhMGQ1NjYyN2ViMzlhZTNjIiwiaCI6Im11cm11cjY0In0=";

        public MainPage()
        {
            InitializeComponent();
            InitMap();
            UpdateLocalizedTexts();
            GetLocationAndCenter();
        }
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            LocalizationManager.Instance.PropertyChanged += OnLocalizationChanged;
            UpdateLocalizedTexts();
            await LoadDataAsync(); // Nạp dữ liệu từ DB (Có await đàng hoàng)
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            LocalizationManager.Instance.PropertyChanged -= OnLocalizationChanged;
        }

        private void OnLocalizationChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(LocalizationManager.CurrentLanguage))
            {
                MainThread.BeginInvokeOnMainThread(UpdateLocalizedTexts);
            }
        }

        private void UpdateLocalizedTexts()
        {
            var lm = LocalizationManager.Instance;
            Title = lm.MapTitle;
            btnGps.Text = lm.GpsButton;
            BtnListen.Text = lm.ListenButton;
            BtnStop.Text = lm.StopButton;
            ApplyCategoryFilter();
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

            // 3. Quét qua DB và nặn ra các điểm POI (Chỉ lấy những điểm đang hoạt động)
            foreach (var loc in danhSachTuDB.Where(l => l.IsActive))
            {
                var poiMoi = new POI
                {
                    Id = loc.Id,
                    ServerId = loc.ServerId, // Lưu ServerId
                    Name = loc.LocationName,
                    Address = loc.Address,
                    ImageUrl = loc.ImageUrl,
                    Latitude = loc.Latitude,
                    Longitude = loc.Longitude
                };
                // Load per-location scripts (langCode -> content)
                try
                {
                    var scripts = await dbService.GetScriptsForLocation(loc.Id);
                    if (scripts != null && scripts.Count > 0)
                    {
                        poiMoi.AudioScripts = scripts;
                    }
                }
                catch { }
                _allPois.Add(poiMoi);
            }

            // 4. Bơm dữ liệu ra UI (Bắt buộc phải nằm trong MainThread)
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Dọn sạch ghim trên bản đồ
                mapView.Pins.Clear();

                // Tự động căn tâm bản đồ vào POI đầu tiên nếu có dữ liệu thực tế
                if (_allPois.Count > 0)
                {
                    var first = _allPois[0];
                    var (x, y) = Mapsui.Projections.SphericalMercator.FromLonLat(first.Longitude, first.Latitude);
                    mapView.Map.Navigator.CenterOnAndZoomTo(new Mapsui.MPoint(x, y), 2.0);
                }

                mapView.Refresh();

                // Cập nhật danh sách hiển thị
                ApplyCategoryFilter();
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
                        var lm = LocalizationManager.Instance;
                        await DisplayAlert(lm.PermissionRequiredTitle, lm.PermissionRequiredMessage, lm.OkButton);
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
                var lm = LocalizationManager.Instance;
                await DisplayAlert(lm.GpsErrorTitle, $"{lm.GpsFetchFailedMessage}\n{ex.Message}", lm.OkButton);
            }
        }

        private void MyLocation_Clicked(object sender, EventArgs e)
        {
            GetLocationAndCenter();
        }


        private void ApplyCategoryFilter()
        {
            // Bây giờ không lọc category nữa, chỉ hiện tất cả các món Active
            cvPoiList.ItemsSource = _allPois.ToList();
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
                var lm = LocalizationManager.Instance;
                await DisplayAlert(lm.ErrorTitle, lm.ApiKeyMissingMessage, lm.OkButton);
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
                var lm = LocalizationManager.Instance;
                await DisplayAlert(lm.RouteErrorTitle, $"{lm.RouteDrawFailedMessage}\n{lm.DetailPrefix}: {ex.Message}", lm.OkButton);
            }
        }

        private async void Speak_Clicked(object sender, EventArgs e) //thuyết minh
        {
            if (_selectedPoiForAudio == null)
            {
                var lmNotice = LocalizationManager.Instance;
                await DisplayAlert(lmNotice.NoticeTitle, lmNotice.SelectPoiNotice, lmNotice.OkButton);
                return;
            }

            var selectedPoi = _selectedPoiForAudio;
            var db = new Services.LocalDbService();
            var allLangs = await db.GetAllLanguages();

            if (allLangs == null || allLangs.Count == 0)
            {
                await DisplayAlert("Thông báo", "Chưa có dữ liệu ngôn ngữ thuyết minh. Vui lòng kiểm tra kết nối!", "OK");
                return;
            }

            // Hiển thị danh sách ngôn ngữ từ DB
            string[] langNames = allLangs.Select(l => l.LangName).ToArray();
            string action = await DisplayActionSheet("Chọn ngôn ngữ thuyết minh", "Hủy", null, langNames);
            
            if (action == "Hủy" || string.IsNullOrEmpty(action)) return;

            var selectedLang = allLangs.FirstOrDefault(l => l.LangName == action);
            if (selectedLang == null) return;

            string lang = selectedLang.LangCode;
            string text = selectedPoi.AudioScripts.GetValueOrDefault(lang, selectedPoi.Name);

            // Bước 3: Chọn Giọng đọc (Lấy từ Server)
            var apiService = IPlatformApplication.Current?.Services.GetService<Services.ApiService>();
            var voices = await apiService.GetVoicesAsync(selectedLang.ServerId);
            Guid? selectedVoiceId = null;

            if (voices != null && voices.Count > 0)
            {
                string[] voiceNames = voices.Select(v => v.DisplayName).ToArray();
                string voiceAction = await DisplayActionSheet("Chọn giọng đọc", "Hủy", null, voiceNames);
                if (voiceAction != "Hủy" && !string.IsNullOrEmpty(voiceAction))
                {
                    selectedVoiceId = voices.FirstOrDefault(v => v.DisplayName == voiceAction)?.Id;
                }
            }

            _speechId++;
            int currentId = _speechId;
            _currentAudioPlayer?.Stop();

            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

                // Gọi API TTS của Server với VoiceId đã chọn
                string apiBase = Preferences.Default.Get("RemoteApiBase", string.Empty);
                if (!string.IsNullOrWhiteSpace(apiBase))
                {
                    string ttsUrl = apiBase.TrimEnd('/') + $"/api/mobile/tts?stallId={selectedPoi.ServerId}&lang={lang}";
                    if (selectedVoiceId.HasValue) ttsUrl += $"&voiceId={selectedVoiceId.Value}";

                    _currentAudioPlayer?.Stop();
                    _currentAudioPlayer = AudioManager.Current.CreatePlayer(ttsUrl);
                    _currentAudioPlayer.Play();
                    return;
                }

                // Fallback nếu không có Server API (Dùng Google TTS cũ)
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

                foreach (var sentence in finalSentences)
                {
                    if (currentId != _speechId) break;
                    string cleanSentence = sentence.Trim();
                    if (string.IsNullOrEmpty(cleanSentence)) continue;

                    var url = $"https://translate.google.com/translate_tts?ie=UTF-8&q={Uri.EscapeDataString(cleanSentence)}&tl={lang}&client=tw-ob";
                    var audioBytes = await client.GetByteArrayAsync(url);
                    
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
                    var apiBaseUrl = Preferences.Default.Get("RemoteApiBase", string.Empty);
                    if (!string.IsNullOrWhiteSpace(apiBaseUrl))
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
                        var lmError = LocalizationManager.Instance;
                        await DisplayAlert(lmError.AudioErrorTitle, lmError.TtsServiceUnavailableMessage, lmError.OkButton);
                    }
                }
                catch
                {
                    var lmError = LocalizationManager.Instance;
                    await DisplayAlert(lmError.AudioErrorTitle, lmError.TtsPlaybackFailedMessage, lmError.OkButton);
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
            _activeCategoryFilter = string.Empty;
            ApplyCategoryFilter();
        }

        private void Filter_Food_Clicked(object sender, EventArgs e)
        {
            _activeCategoryFilter = "Thức ăn";
            ApplyCategoryFilter();
        }

        private void Filter_Travel_Clicked(object sender, EventArgs e)
        {
            _activeCategoryFilter = "Vui chơi";
            ApplyCategoryFilter();
        }

        private void Filter_Event_Clicked(object sender, EventArgs e)
        {
            _activeCategoryFilter = "Lễ hội";
            ApplyCategoryFilter();
        }   
    }

}
