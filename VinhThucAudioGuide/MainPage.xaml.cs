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

        /// <summary>Nhãn góc phải card POI: "Lượt đợi còn: n" — rỗng khi không chờ.</summary>
        private string _queueWaitText = string.Empty;
        public string QueueWaitText
        {
            get => _queueWaitText;
            set
            {
                if (_queueWaitText == value) return;
                _queueWaitText = value ?? string.Empty;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(QueueWaitText)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasQueueWaitLabel)));
            }
        }

        public bool HasQueueWaitLabel => !string.IsNullOrEmpty(_queueWaitText);
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

        /// <summary>Bán kính (mét) để xếp hàng + tự phát thuyết minh một lần khi vào vùng (theo yêu cầu ~3km).</summary>
        private const double AutoNarrationRadiusMeters = 3000;

        private sealed class StallZoneState
        {
            public bool InAutoZone;
            public string? TicketId;
            public bool EnqueuedOnPhone;
        }

        private readonly Dictionary<string, StallZoneState> _stallZoneStates = new(StringComparer.OrdinalIgnoreCase);

        private sealed record GlobalAutoItem(string StallId, string TicketId, POI Poi);

        private readonly List<GlobalAutoItem> _globalAutoQueue = new();
        private readonly object _globalAutoLock = new();
        private readonly SemaphoreSlim _autoProcessorLock = new(1, 1);

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

                        MainThread.BeginInvokeOnMainThread(async () =>
                        {
                            try
                            {
                                await UpdateAutoNarrationZonesAsync(loc);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[AutoNarr] {ex.Message}");
                            }
                            KickAutoNarrationProcessor();
                        });

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
        /// Vùng ~3km quanh POI: vào vùng thì join hàng chờ server + hàng chờ cục bộ (một loa).
        /// Rời vùng thì leave server và gỡ khỏi hàng chờ chưa phát.
        /// </summary>
        private async Task UpdateAutoNarrationZonesAsync(Location userLoc)
        {
            if (_allPois == null || _allPois.Count == 0) return;
            if (!Preferences.Default.Get("AutoPlay", true)) return;

            var api = IPlatformApplication.Current?.Services.GetService<ApiService>();

            var toExit = new List<(string sid, POI poi, StallZoneState st)>();
            var toEnter = new List<(POI poi, string sid, double dist)>();

            foreach (var poi in _allPois)
            {
                if (string.IsNullOrWhiteSpace(poi.AudioUrl)) continue;
                var sid = poi.ServerId?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(sid)) continue;

                if (!_stallZoneStates.TryGetValue(sid, out var st))
                {
                    st = new StallZoneState();
                    _stallZoneStates[sid] = st;
                }

                var dist = HaversineMeters(userLoc.Latitude, userLoc.Longitude, poi.Latitude, poi.Longitude);
                var inside = dist <= AutoNarrationRadiusMeters;

                if (!inside && st.InAutoZone)
                    toExit.Add((sid, poi, st));
                else if (inside && !st.InAutoZone)
                    toEnter.Add((poi, sid, dist));
                else if (inside && st.InAutoZone)
                    await RefreshPoiWaitLabelAsync(poi, sid, api);
            }

            foreach (var (sid, poi, st) in toExit)
                await ExitAutoZoneAsync(sid, poi, st, api);

            toEnter.Sort((a, b) => a.dist.CompareTo(b.dist));
            foreach (var (poi, sid, _) in toEnter)
                await EnterAutoZoneAsync(poi, sid, api);
        }

        private async Task ExitAutoZoneAsync(string sid, POI poi, StallZoneState st, ApiService? api)
        {
            st.InAutoZone = false;
            if (!string.IsNullOrEmpty(st.TicketId))
            {
                if (api != null)
                    await api.NarrationQueueLeaveAsync(sid, st.TicketId);
                lock (_globalAutoLock)
                {
                    _globalAutoQueue.RemoveAll(x =>
                        string.Equals(x.StallId, sid, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(x.TicketId, st.TicketId, StringComparison.Ordinal));
                }
            }

            st.TicketId = null;
            st.EnqueuedOnPhone = false;
            poi.QueueWaitText = string.Empty;
        }

        private async Task EnterAutoZoneAsync(POI poi, string sid, ApiService? api)
        {
            if (!_stallZoneStates.TryGetValue(sid, out var st)) return;
            st.InAutoZone = true;

            NarrationQueueJoinResponse? join = null;
            if (api != null)
                join = await api.NarrationQueueJoinAsync(sid);

            st.TicketId = string.IsNullOrWhiteSpace(join?.TicketId)
                ? $"local-{Guid.NewGuid():N}"
                : join!.TicketId;

            if (!st.EnqueuedOnPhone)
            {
                st.EnqueuedOnPhone = true;
                lock (_globalAutoLock)
                    _globalAutoQueue.Add(new GlobalAutoItem(sid, st.TicketId, poi));
            }

            await RefreshPoiWaitLabelAsync(poi, sid, api);
        }

        private int GetGlobalQueueIndex(string stallId)
        {
            lock (_globalAutoLock)
            {
                var i = _globalAutoQueue.FindIndex(x =>
                    string.Equals(x.StallId, stallId, StringComparison.OrdinalIgnoreCase));
                return i < 0 ? 0 : i;
            }
        }

        private async Task RefreshPoiWaitLabelAsync(POI poi, string sid, ApiService? api)
        {
            var lm = LocalizationManager.Instance;
            var serverAhead = 0;
            if (api != null)
            {
                var a = await api.NarrationQueueGetAheadAsync(sid);
                serverAhead = a ?? 0;
            }

            var gIdx = GetGlobalQueueIndex(sid);
            var total = serverAhead + gIdx;
            var text = total > 0 ? string.Format(lm.QueueWaitRemainingFormat, total) : string.Empty;
            await MainThread.InvokeOnMainThreadAsync(() => poi.QueueWaitText = text);
        }

        private void KickAutoNarrationProcessor()
        {
            _ = Task.Run(ProcessAutoNarrationQueueLoopAsync);
        }

        private async Task ProcessAutoNarrationQueueLoopAsync()
        {
            await _autoProcessorLock.WaitAsync();
            try
            {
                var api = IPlatformApplication.Current?.Services.GetService<ApiService>();

                while (true)
                {
                    GlobalAutoItem? item = null;
                    lock (_globalAutoLock)
                    {
                        if (_globalAutoQueue.Count > 0)
                            item = _globalAutoQueue[0];
                    }

                    if (item == null)
                        break;

                    var loc = _currentUserLocation;
                    if (loc != null)
                    {
                        var dist = HaversineMeters(loc.Latitude, loc.Longitude, item.Poi.Latitude, item.Poi.Longitude);
                        if (dist > AutoNarrationRadiusMeters)
                        {
                            await CleanupStaleHeadItemAsync(api, item);
                            continue;
                        }
                    }

                    int? serverAheadNullable = api != null ? await api.NarrationQueueGetAheadAsync(item.StallId) : 0;
                    var serverAhead = serverAheadNullable ?? 0;
                    var gIdx = GetGlobalQueueIndex(item.StallId);
                    var waitTotal = serverAhead + gIdx;

                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        var lm = LocalizationManager.Instance;
                        item.Poi.QueueWaitText = waitTotal > 0
                            ? string.Format(lm.QueueWaitRemainingFormat, waitTotal)
                            : string.Empty;
                    });

                    if (serverAhead > 0 || gIdx > 0)
                    {
                        await Task.Delay(1500);
                        continue;
                    }

                    await MainThread.InvokeOnMainThreadAsync(() => item.Poi.QueueWaitText = string.Empty);

                    var playId = await PlayPoiAudioAsync(item.Poi, manualTrigger: false);

                    if (playId.HasValue)
                        await WaitForPlaybackSessionEndAsync(playId.Value);

                    if (api != null)
                        await api.NarrationQueueCompleteAsync(item.StallId, item.TicketId);

                    lock (_globalAutoLock)
                    {
                        if (_globalAutoQueue.Count > 0
                            && string.Equals(_globalAutoQueue[0].TicketId, item.TicketId, StringComparison.Ordinal)
                            && string.Equals(_globalAutoQueue[0].StallId, item.StallId, StringComparison.Ordinal))
                            _globalAutoQueue.RemoveAt(0);
                    }

                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        if (_stallZoneStates.TryGetValue(item.StallId, out var s)
                            && string.Equals(s.TicketId, item.TicketId, StringComparison.Ordinal))
                        {
                            s.TicketId = null;
                            s.EnqueuedOnPhone = false;
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AutoNarrLoop] {ex.Message}");
            }
            finally
            {
                _autoProcessorLock.Release();
            }
        }

        private async Task CleanupStaleHeadItemAsync(ApiService? api, GlobalAutoItem item)
        {
            if (api != null)
                await api.NarrationQueueLeaveAsync(item.StallId, item.TicketId);

            lock (_globalAutoLock)
            {
                if (_globalAutoQueue.Count > 0
                    && string.Equals(_globalAutoQueue[0].TicketId, item.TicketId, StringComparison.Ordinal))
                    _globalAutoQueue.RemoveAt(0);
            }

            await MainThread.InvokeOnMainThreadAsync(() => item.Poi.QueueWaitText = string.Empty);
        }

        private async Task WaitForPlaybackSessionEndAsync(int playSessionId)
        {
            for (var n = 0; n < 4000; n++)
            {
                await Task.Delay(100);
                if (_speechId != playSessionId)
                    return;
                try
                {
                    if (_currentAudioPlayer == null || !_currentAudioPlayer.IsPlaying)
                        return;
                }
                catch
                {
                    return;
                }
            }
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
        ///  - Hang cho tu dong (GPS trong vung ~3km)
        /// </summary>
        /// <returns>Ma phien phat (_speechId) neu da bat dau phat; null neu khong phat duoc.</returns>
        private async Task<int?> PlayPoiAudioAsync(POI poi, bool manualTrigger)
        {
            if (poi == null) return null;

            if (string.IsNullOrWhiteSpace(poi.AudioUrl))
            {
                if (manualTrigger)
                {
                    var lmAudio = LocalizationManager.Instance;
                    await DisplayAlert(lmAudio.PoiMissingAudioTitle, lmAudio.PoiMissingAudioMessage, lmAudio.OkButton);
                }
                return null;
            }

            _speechId++;
            int currentId = _speechId;
            _currentAudioPlayer?.Stop();

            try
            {
                var bytes = await Services.ApiService.DownloadAudioAsync(poi.AudioUrl);
                if (currentId != _speechId) return null;
                if (bytes == null || bytes.Length == 0)
                {
                    if (manualTrigger)
                    {
                        var lm = LocalizationManager.Instance;
                        await DisplayAlert(lm.AudioErrorTitle, lm.TtsPlaybackFailedMessage, lm.OkButton);
                    }
                    return null;
                }

                _currentAudioPlayer?.Stop();
                _currentAudioPlayer = AudioManager.Current.CreatePlayer(new MemoryStream(bytes));
                _currentAudioPlayer.Play();
                return currentId;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PlayPoiAudio] {ex.Message}");
                if (manualTrigger)
                {
                    var lm = LocalizationManager.Instance;
                    await DisplayAlert(lm.AudioErrorTitle, ex.Message, lm.OkButton);
                }
                return null;
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
