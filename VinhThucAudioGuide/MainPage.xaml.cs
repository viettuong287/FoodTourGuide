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
using System.Collections.Generic;
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

        // Background GPS tracking
        private CancellationTokenSource _locationCts;
        private readonly List<LocationLogEntry> _locationBuffer = [];
        private readonly object _locationLock = new();

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
            await LoadDataAsync();
            StartLocationTracking();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            LocalizationManager.Instance.PropertyChanged -= OnLocalizationChanged;
            StopLocationTracking();
        }

        private void StartLocationTracking()
        {
            _locationCts?.Cancel();
            _locationCts = new CancellationTokenSource();
            _ = Task.Run(() => RunLocationTrackingAsync(_locationCts.Token));
        }

        private void StopLocationTracking()
        {
            _locationCts?.Cancel();
            _locationCts = null;
        }

        private async Task RunLocationTrackingAsync(CancellationToken ct)
        {
            var apiService = IPlatformApplication.Current?.Services.GetService<Services.ApiService>();
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(20000, ct);

                    var loc = await Geolocation.Default.GetLocationAsync(
                        new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(5)), ct);

                    if (loc != null)
                    {
                        _currentUserLocation = loc;
                        MainThread.BeginInvokeOnMainThread(() =>
                            mapView.MyLocationLayer.UpdateMyLocation(
                                new Mapsui.UI.Maui.Position(loc.Latitude, loc.Longitude)));

                        var entry = new Services.LocationLogEntry
                        {
                            Latitude = loc.Latitude,
                            Longitude = loc.Longitude,
                            Accuracy = loc.Accuracy,
                            RecordedAt = DateTimeOffset.Now
                        };

                        List<Services.LocationLogEntry> toSend;
                        lock (_locationLock)
                        {
                            _locationBuffer.Add(entry);
                            toSend = [.. _locationBuffer];
                            _locationBuffer.Clear();
                        }

                        if (apiService != null && toSend.Count > 0)
                            await apiService.SendLocationBatchAsync(toSend);
                    }
                }
                catch (OperationCanceledException) { break; }
                catch { /* bỏ qua lỗi GPS nền */ }
            }
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
            _allPois = new List<POI>();
            var dbService = new VinhThucAudioGuide.Services.LocalDbService();
            var apiService = IPlatformApplication.Current?.Services.GetService<Services.ApiService>();

            // Thử tải danh sách gian hàng từ API trước
            bool loadedFromApi = false;
            if (apiService != null)
            {
                var stalls = await apiService.GetStallsAsync();
                if (stalls != null && stalls.Count > 0)
                {
                    foreach (var stall in stalls)
                    {
                        var poi = new POI
                        {
                            ServerId = stall.StallId,
                            Name = stall.StallName,
                            Address = stall.NarrationContent?.Description ?? string.Empty,
                            ImageUrl = stall.MediaImages.Count > 0 ? stall.MediaImages[0].Url : string.Empty,
                            Latitude = stall.Latitude,
                            Longitude = stall.Longitude
                        };
                        // Nạp script âm thanh từ DB cục bộ nếu có
                        try
                        {
                            var localLoc = await dbService.GetTourLocationByServerId(stall.StallId);
                            if (localLoc != null)
                            {
                                poi.Id = localLoc.Id;
                                poi.AudioScripts = await dbService.GetScriptsForLocation(localLoc.Id);
                            }
                        }
                        catch { }
                        _allPois.Add(poi);
                    }
                    loadedFromApi = true;
                }
            }

            // Fallback: tải từ DB cục bộ nếu API không có dữ liệu
            if (!loadedFromApi)
            {
                var danhSachTuDB = await dbService.GetAllTourLocations();
                foreach (var loc in danhSachTuDB.Where(l => l.IsActive))
                {
                    var poi = new POI
                    {
                        Id = loc.Id,
                        ServerId = loc.ServerId,
                        Name = loc.LocationName,
                        Address = loc.Address,
                        ImageUrl = loc.ImageUrl,
                        Latitude = loc.Latitude,
                        Longitude = loc.Longitude
                    };
                    try
                    {
                        var scripts = await dbService.GetScriptsForLocation(loc.Id);
                        if (scripts != null && scripts.Count > 0)
                            poi.AudioScripts = scripts;
                    }
                    catch { }
                    _allPois.Add(poi);
                }
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                mapView.Pins.Clear();

                if (_allPois.Count > 0)
                {
                    var first = _allPois[0];
                    var (x, y) = Mapsui.Projections.SphericalMercator.FromLonLat(first.Longitude, first.Latitude);
                    mapView.Map.Navigator.CenterOnAndZoomTo(new Mapsui.MPoint(x, y), 2.0);
                }

                mapView.Refresh();
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
            var voices = new List<Services.SyncVoiceProfile>();
            if (apiService != null && !string.IsNullOrEmpty(selectedLang.ServerId))
                voices = await apiService.GetVoicesAsync(selectedLang.ServerId);

            Guid? selectedVoiceId = null;
            if (voices != null && voices.Count > 0)
            {
                string[] voiceNames = voices.Select(v => v.DisplayName).ToArray();
                string voiceAction = await DisplayActionSheet("Chọn giọng đọc", "Hủy", null, voiceNames);
                if (voiceAction != "Hủy" && !string.IsNullOrEmpty(voiceAction))
                    selectedVoiceId = voices.FirstOrDefault(v => v.DisplayName == voiceAction)?.Id;
            }

            _speechId++;
            int currentId = _speechId;
            _currentAudioPlayer?.Stop();

            // ===== Nguồn 1: Server TTS API =====
            string apiBase = Preferences.Default.Get("RemoteApiBase", string.Empty);
            if (!string.IsNullOrWhiteSpace(apiBase) && !string.IsNullOrEmpty(selectedPoi.ServerId))
            {
                try
                {
                    using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
                    string ttsUrl = apiBase.TrimEnd('/') + $"/api/mobile/tts?stallId={selectedPoi.ServerId}&lang={lang}";
                    if (selectedVoiceId.HasValue) ttsUrl += $"&voiceId={selectedVoiceId.Value}";

                    var ttsBytes = await client.GetByteArrayAsync(ttsUrl);
                    if (ttsBytes != null && ttsBytes.Length > 0)
                    {
                        _currentAudioPlayer?.Stop();
                        _currentAudioPlayer = AudioManager.Current.CreatePlayer(new MemoryStream(ttsBytes));
                        _currentAudioPlayer.Play();

                        // Cache best-effort
                        _ = Services.AudioCacheService.GetOrFetchAudioAsync(selectedPoi.ServerId, lang);
                        return;
                    }
                }
                catch { /* Server lỗi → thử nguồn tiếp theo */ }
            }

            // ===== Nguồn 2: Google Translate TTS =====
            if (!string.IsNullOrWhiteSpace(text))
            {
                bool playedFromGoogle = false;
                try
                {
                    using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

                    var rawParts = text.Split(new[] { '.', '\n', '?', '!' }, StringSplitOptions.RemoveEmptyEntries);
                    var finalSentences = new List<string>();
                    foreach (var part in rawParts)
                    {
                        var words = part.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                        string chunk = "";
                        foreach (var word in words)
                        {
                            if (chunk.Length + word.Length > 150) { finalSentences.Add(chunk.Trim()); chunk = ""; }
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
                        playedFromGoogle = true;

                        await Task.Delay(300);
                        while (_currentAudioPlayer.IsPlaying && currentId == _speechId)
                            await Task.Delay(100);
                    }
                }
                catch { /* Google block → thử nguồn tiếp theo */ }

                if (playedFromGoogle) return;
            }

            // ===== Nguồn 3: MAUI built-in TextToSpeech (không cần DependencyService) =====
            try
            {
                var locales = await TextToSpeech.Default.GetLocalesAsync();
                var matchedLocale = locales.FirstOrDefault(l =>
                    l.Language.StartsWith(lang, StringComparison.OrdinalIgnoreCase));

                await TextToSpeech.Default.SpeakAsync(text, new SpeechOptions { Locale = matchedLocale });
            }
            catch
            {
                var lmError = LocalizationManager.Instance;
                await DisplayAlert(lmError.AudioErrorTitle, lmError.TtsPlaybackFailedMessage, lmError.OkButton);
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
