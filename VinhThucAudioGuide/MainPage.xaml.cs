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
    public class POI : INotifyPropertyChanged
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

        // URL audio đã được API /api/geo/stalls filter sẵn theo ngôn ngữ + giọng đọc
        // trong DevicePreference. Mobile chỉ cần download và phát, không cần biết về ngôn ngữ.
        public string? AudioUrl { get; set; }

        // Bán kính (mét) để tự động phát thuyết minh khi user vào trong vùng POI.
        // Lấy từ GeoStallDto.RadiusMeters. Mặc định 50m nếu API không trả về.
        public double RadiusMeters { get; set; } = 50;

        // Trạng thái được chọn - dùng cho DataTrigger trong XAML để đổi màu card.
        // Khi user bấm 1 POI thì IsSelected = true, bấm lại sẽ về false (toggle).
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value) return;
                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
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
        // POI vua duoc auto-play do GPS gan day. Reset khi user roi khoi vung,
        // de lan toi quay lai se phat lai. Tranh spam phat lap khi user dung tai cho.
        private string? _lastAutoPlayedPoiServerId;

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

                        // Kiem tra GPS co vao trong vung POI nao khong -> auto-play
                        CheckProximityAutoPlay(loc);

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

        /// <summary>
        /// Khi GPS cap nhat, kiem tra xem user co vao trong vung RadiusMeters cua POI nao khong.
        /// Neu co va POI do khac POI vua phat thi tu dong phat thuyet minh.
        /// Toggle "AutoPlay" trong Settings co the tat tinh nang nay.
        /// </summary>
        private void CheckProximityAutoPlay(Location userLoc)
        {
            if (_allPois == null || _allPois.Count == 0) return;
            if (!Preferences.Default.Get("AutoPlay", true)) return;

            POI? insidePoi = null;
            double bestDistance = double.MaxValue;
            foreach (var poi in _allPois)
            {
                if (string.IsNullOrWhiteSpace(poi.AudioUrl)) continue; // POI chua co audio thi bo qua
                double dist = HaversineMeters(userLoc.Latitude, userLoc.Longitude, poi.Latitude, poi.Longitude);
                double radius = poi.RadiusMeters > 0 ? poi.RadiusMeters : 50;
                if (dist <= radius && dist < bestDistance)
                {
                    insidePoi = poi;
                    bestDistance = dist;
                }
            }

            if (insidePoi == null)
            {
                // Ra khoi moi vung POI -> reset de lan toi vao lai se phat moi
                _lastAutoPlayedPoiServerId = null;
                return;
            }

            if (string.Equals(insidePoi.ServerId, _lastAutoPlayedPoiServerId, StringComparison.OrdinalIgnoreCase))
                return; // van dang trong vung POI vua phat -> khong phat lap

            _lastAutoPlayedPoiServerId = insidePoi.ServerId;
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await PlayPoiAudioAsync(insidePoi, manualTrigger: false);
            });
        }

        /// <summary>
        /// Tinh khoang cach (met) giua 2 toa do GPS bang cong thuc Haversine (chinh xac tren mat cau).
        /// Ban kinh Trai Dat dung 6,371,000 m. Du cho khoang cach vai chuc met.
        /// </summary>
        private static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6_371_000;
            double dLat = (lat2 - lat1) * Math.PI / 180.0;
            double dLon = (lon2 - lon1) * Math.PI / 180.0;
            double sinDLat = Math.Sin(dLat / 2);
            double sinDLon = Math.Sin(dLon / 2);
            double a = sinDLat * sinDLat
                     + Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0)
                     * sinDLon * sinDLon;
            return 2 * R * Math.Asin(Math.Min(1, Math.Sqrt(a)));
        }

        private void UpdateLocalizedTexts()
        {
            var lm = LocalizationManager.Instance;
            Title = lm.MapTitle;
            btnGps.Text = lm.GpsButton;
            BtnListen.Text = lm.ListenButton;
            BtnStop.Text = lm.StopButton;
            // Tiêu đề danh sách POI + nội dung empty state cũng cần đổi ngôn ngữ theo pref hiện tại.
            if (LblPoiListTitle != null) LblPoiListTitle.Text = lm.FoodCategory;
            if (LblPoiEmptyTitle != null) LblPoiEmptyTitle.Text = lm.MapEmpty;
            if (LblPoiEmptyHint != null) LblPoiEmptyHint.Text = lm.OnboardingCheckNetwork;
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
            // Dùng singleton từ DI thay vì new để chia sẻ connection SQLite và tránh khởi tạo trùng schema.
            var dbService = IPlatformApplication.Current?.Services.GetService<Services.LocalDbService>()
                            ?? new Services.LocalDbService();
            var apiService = IPlatformApplication.Current?.Services.GetService<Services.ApiService>();

            // Tai danh sach stall tu API. Server da filter NarrationContent + AudioUrl
            // theo DevicePreference (ngon ngu + giong doc user da chon o LanguageSelectionPage).
            // Mobile chi can dung truc tiep AudioUrl, khong can biet ve ngon ngu.
            bool loadedFromApi = false;
            if (apiService != null)
            {
                var stalls = await apiService.GetStallsAsync();
                if (stalls != null && stalls.Count > 0)
                {
                    foreach (var stall in stalls)
                    {
                        var nc = stall.NarrationContent;
                        var poi = new POI
                        {
                            ServerId     = stall.StallId,
                            Name         = stall.StallName,
                            Address      = nc?.Description ?? string.Empty,
                            ImageUrl     = stall.MediaImages.Count > 0 ? stall.MediaImages[0].Url : string.Empty,
                            Latitude     = stall.Latitude,
                            Longitude    = stall.Longitude,
                            RadiusMeters = stall.RadiusMeters > 0 ? stall.RadiusMeters : 50,
                            AudioUrl     = nc?.AudioUrl
                        };

                        // Lay them Id cuc bo neu da sync truoc do (chi dung cho ListView highlighting).
                        try
                        {
                            var localLoc = await dbService.GetTourLocationByServerId(stall.StallId);
                            if (localLoc != null) poi.Id = localLoc.Id;
                        }
                        catch { }

                        _allPois.Add(poi);
                    }
                    loadedFromApi = true;
                }
            }

            // Fallback offline: tai POI tu local DB. AudioUrl se rong, user can co mang lan dau de co audio.
            if (!loadedFromApi)
            {
                var danhSachTuDB = await dbService.GetAllTourLocations();
                foreach (var loc in danhSachTuDB.Where(l => l.IsActive))
                {
                    _allPois.Add(new POI
                    {
                        Id        = loc.Id,
                        ServerId  = loc.ServerId,
                        Name      = loc.LocationName,
                        Address   = loc.Address,
                        ImageUrl  = loc.ImageUrl,
                        Latitude  = loc.Latitude,
                        Longitude = loc.Longitude
                    });
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

            // Clear SelectedItem ngay để: (1) tự render highlight qua IsSelected thay cho
            // selection mặc định của CollectionView, (2) lần bấm tiếp theo lên cùng item vẫn
            // kích hoạt SelectionChanged → cho phép logic toggle hoạt động.
            cvPoiList.SelectedItem = null;

            // Toggle: nếu bấm lại đúng POI đang chọn thì bỏ chọn, xoá pin + route.
            if (_selectedPoiForAudio == target)
            {
                target.IsSelected = false;
                _selectedPoiForAudio = null;
                mapView.Pins.Clear();
                _routeLayer.Features = new List<IFeature>();
                _routeLayer.DataHasChanged();
                mapView.Refresh();
                return;
            }

            // Bỏ chọn POI cũ (nếu có), sau đó đánh dấu POI mới đang chọn.
            if (_selectedPoiForAudio != null)
                _selectedPoiForAudio.IsSelected = false;

            target.IsSelected = true;
            _selectedPoiForAudio = target;

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

        // Bam nut Phat thuyet minh: chi don gian phat AudioUrl da co san tu /api/geo/stalls.
        // Server da filter theo DevicePreference (ngon ngu + giong doc user chon o LanguageSelectionPage),
        // mobile khong can quan tam ngon ngu nao -> khong action sheet, khong TTS fallback.
        private async void Speak_Clicked(object sender, EventArgs e)
        {
            if (_selectedPoiForAudio == null)
            {
                var lm = LocalizationManager.Instance;
                await DisplayAlert(lm.NoticeTitle, lm.SelectPoiNotice, lm.OkButton);
                return;
            }
            await PlayPoiAudioAsync(_selectedPoiForAudio, manualTrigger: true);
        }

        /// <summary>
        /// Phat thuyet minh cua mot POI. Goi tu:
        ///  - Speak_Clicked (user bam nut)
        ///  - CheckProximityAutoPlay (GPS vao trong RadiusMeters cua POI)
        /// </summary>
        /// <param name="poi">POI can phat. AudioUrl phai khac null.</param>
        /// <param name="manualTrigger">true neu user chu dong bam nut -> hien alert khi loi.
        /// false neu auto-play do gan POI -> im lang khi khong co audio.</param>
        private async Task PlayPoiAudioAsync(POI poi, bool manualTrigger)
        {
            if (poi == null) return;

            if (string.IsNullOrWhiteSpace(poi.AudioUrl))
            {
                if (manualTrigger)
                {
                    var lmAudio = LocalizationManager.Instance;
                    await DisplayAlert(lmAudio.PoiMissingAudioTitle, lmAudio.PoiMissingAudioMessage, lmAudio.OkButton);
                }
                return;
            }

            _speechId++;
            int currentId = _speechId;
            _currentAudioPlayer?.Stop();

            try
            {
                var bytes = await Services.ApiService.DownloadAudioAsync(poi.AudioUrl);
                if (currentId != _speechId) return; // user da bam Stop hoac chuyen POI
                if (bytes == null || bytes.Length == 0)
                {
                    if (manualTrigger)
                    {
                        var lm = LocalizationManager.Instance;
                        await DisplayAlert(lm.AudioErrorTitle, lm.TtsPlaybackFailedMessage, lm.OkButton);
                    }
                    return;
                }

                _currentAudioPlayer?.Stop();
                _currentAudioPlayer = AudioManager.Current.CreatePlayer(new MemoryStream(bytes));
                _currentAudioPlayer.Play();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PlayPoiAudio] {ex.Message}");
                if (manualTrigger)
                {
                    var lm = LocalizationManager.Instance;
                    await DisplayAlert(lm.AudioErrorTitle, ex.Message, lm.OkButton);
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
