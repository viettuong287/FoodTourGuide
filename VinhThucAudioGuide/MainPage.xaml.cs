using Mapsui;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
using Microsoft.Maui.Devices.Sensors;
using Mapsui.Layers;
using Mapsui.Providers;
using Mapsui.Styles;
using System.Text.Json;
using System.Net.Http;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Media;
using System.Threading;

namespace VinhThucAudioGuide
{
    public class POI
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public Microsoft.Maui.Graphics.Color PinColor { get; set; }
        public Dictionary<string, string> AudioScripts { get; set; } = new Dictionary<string, string>();
    }

    public partial class MainPage : ContentPage
    {
        private List<POI> _poiList = new List<POI>();
        private Location _currentUserLocation;
        private MemoryLayer _routeLayer;
        private CancellationTokenSource _speechCancelToken;

        // API Key của bro đã được dán sẵn
        private readonly string ORS_API_KEY = "eyJvcmciOiI1YjNjZTM1OTc4NTExMTAwMDFjZjYyNDgiLCJpZCI6IjM4MTUxY2VmYzJmZDQ1ZDdhMGQ1NjYyN2ViMzlhZTNjIiwiaCI6Im11cm11cjY0In0=";

        public MainPage()
        {
            InitializeComponent();
            InitMap();
            LoadMockData();
        }

        private void InitMap()
        {
            var map = new Mapsui.Map();
            map.Layers.Add(Mapsui.Tiling.OpenStreetMap.CreateTileLayer());
            map.Widgets.Clear();
            // Đã sửa lỗi: Dùng đúng tên mapView như trong XAML
            mapView.Map = map;
        }

        private void LoadMockData()
        {
            var banhMi = new POI { Name = "Bánh mì Huỳnh Hoa", Category = "Ẩm thực", Latitude = 10.7716, Longitude = 106.6923, PinColor = Microsoft.Maui.Graphics.Colors.Orange };
            banhMi.AudioScripts.Add("vi", "Bánh mì Huỳnh Hoa là tiệm bánh mì nổi tiếng nhất Sài Gòn, nổi bật với lớp nhân thịt nguội và pate siêu dày. Bạn nhất định phải thử một lần khi đến đây.");
            banhMi.AudioScripts.Add("th", "บั๋นหมี่ ฮวี่นฮวา เป็นร้านแซนด์วิชเวียดนามที่มีชื่อเสียงที่สุดในไซง่อน มีชื่อเสียงในเรื่องปาเต้และโคลด์คัทที่อัดแน่น");
            banhMi.AudioScripts.Add("en-US", "Huynh Hoa Bakery is the most famous Vietnamese sandwich shop in Saigon, known for its super thick pate and cold cuts.");
            banhMi.AudioScripts.Add("en-AU", "G'day mate! Huynh Hoa Bakery is the absolute best Banh Mi joint in Saigon. The pate is bloody brilliant!");
            banhMi.AudioScripts.Add("en-CA", "Huynh Hoa Bakery is the most famous sandwich shop in Saigon. You've gotta try their pate, eh!");

            _poiList.Add(banhMi);
            _poiList.Add(new POI { Name = "Bảo tàng Lịch sử TP.HCM", Category = "Du lịch", Latitude = 10.7880, Longitude = 106.7046, PinColor = Microsoft.Maui.Graphics.Colors.Blue });
            _poiList.Add(new POI { Name = "Dinh Độc Lập", Category = "Du lịch", Latitude = 10.7769, Longitude = 106.6951, PinColor = Microsoft.Maui.Graphics.Colors.Blue });
            _poiList.Add(new POI { Name = "Chợ Bến Thành", Category = "Ẩm thực", Latitude = 10.7725, Longitude = 106.6980, PinColor = Microsoft.Maui.Graphics.Colors.Orange });
            _poiList.Add(new POI { Name = "Concert Anh Trai Say Hi", Category = "Sự kiện", Latitude = 10.7985, Longitude = 106.6668, PinColor = Microsoft.Maui.Graphics.Colors.Purple });
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await FocusOnUserLocationAsync();
        }

        private async Task FocusOnUserLocationAsync()
        {
            try
            {
                var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
                var location = await Geolocation.Default.GetLocationAsync(request);

                if (location != null)
                {
                    _currentUserLocation = location;
                    var (x, y) = Mapsui.Projections.SphericalMercator.FromLonLat(location.Longitude, location.Latitude);
                    mapView.Map.Navigator.CenterOnAndZoomTo(new Mapsui.MPoint(x, y), 3.0);
                    mapView.MyLocationEnabled = true;
                    mapView.MyLocationLayer.UpdateMyLocation(new Mapsui.UI.Maui.Position(location.Latitude, location.Longitude));
                }

                Geolocation.Default.LocationChanged += OnLocationChanged;
                var listeningRequest = new GeolocationListeningRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(5));
                await Geolocation.Default.StartListeningForegroundAsync(listeningRequest);
            }
            catch { }
        }

        private void OnLocationChanged(object sender, GeolocationLocationChangedEventArgs e)
        {
            if (e.Location != null)
            {
                _currentUserLocation = e.Location;
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    mapView.MyLocationLayer.UpdateMyLocation(new Mapsui.UI.Maui.Position(e.Location.Latitude, e.Location.Longitude));
                    if (lblPoiName.Text != "Vui lòng chọn danh mục" && !lblPoiName.Text.StartsWith("Đang xem:"))
                    {
                        var targetPoi = _poiList.FirstOrDefault(p => p.Name == lblPoiName.Text);
                        if (targetPoi != null)
                        {
                            double dist = Location.CalculateDistance(_currentUserLocation, new Location(targetPoi.Latitude, targetPoi.Longitude), DistanceUnits.Kilometers);
                            lblDistance.Text = $"Cách bạn: {Math.Round(dist, 2)} km";
                        }
                    }
                });
            }
        }

        private void Category_Clicked(object sender, EventArgs e)
        {
            if (sender is Button btn)
            {
                string categoryName = btn.Text.Substring(3).Trim();
                cvPoiList.ItemsSource = _poiList.Where(p => p.Category == categoryName).ToList();
                lblPoiName.Text = $"Đang xem: {categoryName}";
                lblDistance.Text = "Hãy chọn địa điểm trong danh sách";
            }
        }

        private void Poi_Selected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is POI targetPoi)
            {
                mapView.Pins.Clear();
                if (_routeLayer != null) { mapView.Map.Layers.Remove(_routeLayer); _routeLayer = null; }

                mapView.Pins.Add(new Mapsui.UI.Maui.Pin(mapView)
                {
                    Position = new Mapsui.UI.Maui.Position(targetPoi.Latitude, targetPoi.Longitude),
                    Type = Mapsui.UI.Maui.PinType.Pin,
                    Label = targetPoi.Name,
                    Color = targetPoi.PinColor
                });

                var (x, y) = Mapsui.Projections.SphericalMercator.FromLonLat(targetPoi.Longitude, targetPoi.Latitude);
                var currentZoom = mapView.Map.Navigator.Viewport.Resolution;
                mapView.Map.Navigator.FlyTo(new Mapsui.MPoint(x, y), currentZoom, 500);

                lblPoiName.Text = targetPoi.Name;
                if (_currentUserLocation != null)
                {
                    double dist = Location.CalculateDistance(_currentUserLocation, new Location(targetPoi.Latitude, targetPoi.Longitude), DistanceUnits.Kilometers);
                    lblDistance.Text = $"Cách bạn: {Math.Round(dist, 2)} km";
                }
            }
        }

        private void MyLocation_Clicked(object sender, EventArgs e)
        {
            if (_currentUserLocation != null)
            {
                var (x, y) = Mapsui.Projections.SphericalMercator.FromLonLat(_currentUserLocation.Longitude, _currentUserLocation.Latitude);
                var currentZoom = mapView.Map.Navigator.Viewport.Resolution;
                mapView.Map.Navigator.FlyTo(new Mapsui.MPoint(x, y), currentZoom, 500);
            }
        }

        private async void Navigate_Clicked(object sender, EventArgs e)
        {
            if (cvPoiList.SelectedItem is POI selectedPoi && _currentUserLocation != null)
            {
                lblDistance.Text = "Đang vẽ đường...";
                await DrawRouteAsync(_currentUserLocation.Latitude, _currentUserLocation.Longitude, selectedPoi.Latitude, selectedPoi.Longitude);
            }
        }

        private async Task DrawRouteAsync(double startLat, double startLon, double endLat, double endLon)
        {
            try
            {
                string url = $"https://api.openrouteservice.org/v2/directions/driving-car?api_key={ORS_API_KEY}&start={startLon.ToString(System.Globalization.CultureInfo.InvariantCulture)},{startLat.ToString(System.Globalization.CultureInfo.InvariantCulture)}&end={endLon.ToString(System.Globalization.CultureInfo.InvariantCulture)},{endLat.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
                using HttpClient client = new HttpClient();
                string json = await client.GetStringAsync(url);
                using JsonDocument doc = JsonDocument.Parse(json);
                var coords = doc.RootElement.GetProperty("features")[0].GetProperty("geometry").GetProperty("coordinates");

                List<NetTopologySuite.Geometries.Coordinate> routePoints = new List<NetTopologySuite.Geometries.Coordinate>();
                foreach (var c in coords.EnumerateArray())
                {
                    var (x, y) = Mapsui.Projections.SphericalMercator.FromLonLat(c[0].GetDouble(), c[1].GetDouble());
                    routePoints.Add(new NetTopologySuite.Geometries.Coordinate(x, y));
                }

                if (_routeLayer != null) mapView.Map.Layers.Remove(_routeLayer);

                var lineString = new NetTopologySuite.Geometries.LineString(routePoints.ToArray());
                var lineFeature = new Mapsui.Nts.GeometryFeature(lineString);
                lineFeature.Styles.Add(new Mapsui.Styles.VectorStyle { Line = new Mapsui.Styles.Pen(Microsoft.Maui.Graphics.Colors.Blue.ToMapsui(), 5) });

                _routeLayer = new MemoryLayer { Name = "RouteLayer", Features = new[] { lineFeature } };
                mapView.Map.Layers.Add(_routeLayer);
                mapView.Refresh();

                if (_routeLayer.Extent != null) mapView.Map.Navigator.ZoomToBox(_routeLayer.Extent.Grow(1000));
                lblDistance.Text = "Đã vẽ xong đường đi!";
            }
            catch (Exception ex) { await DisplayAlert("Lỗi", ex.Message, "OK"); }
        }

        private async void Speak_Clicked(object sender, EventArgs e)
        {
            if (cvPoiList.SelectedItem is POI selectedPoi)
            {
                string action = await DisplayActionSheet("Chọn ngôn ngữ thuyết minh", "Hủy", null,
                    "🇻🇳 Tiếng Việt", "🇹🇭 Tiếng Thái", "🇺🇸 Tiếng Anh (Mỹ)", "🇦🇺 Tiếng Anh (Úc)", "🇨🇦 Tiếng Anh (Canada)");

                if (action == "Hủy" || string.IsNullOrEmpty(action)) return;

                string textToRead = "";
                string langSearch = "";
                string countrySearch = "";

                if (action.Contains("Việt")) { textToRead = selectedPoi.AudioScripts.GetValueOrDefault("vi", $"Bạn đang ở {selectedPoi.Name}"); langSearch = "vi"; }
                else if (action.Contains("Thái")) { textToRead = selectedPoi.AudioScripts.GetValueOrDefault("th", $"คุณอยู่ที่ {selectedPoi.Name}"); langSearch = "th"; }
                else if (action.Contains("Mỹ")) { textToRead = selectedPoi.AudioScripts.GetValueOrDefault("en-US", $"Welcome to {selectedPoi.Name}"); langSearch = "en"; countrySearch = "US"; }
                else if (action.Contains("Úc")) { textToRead = selectedPoi.AudioScripts.GetValueOrDefault("en-AU", $"G'day! You're at {selectedPoi.Name}"); langSearch = "en"; countrySearch = "AU"; }
                else if (action.Contains("Canada")) { textToRead = selectedPoi.AudioScripts.GetValueOrDefault("en-CA", $"Welcome to {selectedPoi.Name}, eh?"); langSearch = "en"; countrySearch = "CA"; }

                if (_speechCancelToken != null && !_speechCancelToken.IsCancellationRequested) _speechCancelToken.Cancel();
                _speechCancelToken = new CancellationTokenSource();

                try
                {
                    var locales = await TextToSpeech.Default.GetLocalesAsync();
                    var locale = locales.FirstOrDefault(l => l.Language.ToLower().Contains(langSearch) &&
                                                            (string.IsNullOrEmpty(countrySearch) || l.Country.ToLower().Contains(countrySearch.ToLower())))
                                 ?? locales.FirstOrDefault(l => l.Language.ToLower().Contains(langSearch));

                    if (locale == null)
                    {
                        await DisplayAlert("Thông báo", $"Máy bạn chưa cài gói ngôn ngữ cho {action}. Vui lòng cài đặt trong Settings.", "OK");
                        return;
                    }

                    await TextToSpeech.Default.SpeakAsync(textToRead, new SpeechOptions { Locale = locale, Pitch = 1.0f, Volume = 1.0f }, cancelToken: _speechCancelToken.Token);
                }
                catch (Exception ex) { await DisplayAlert("Lỗi", "Không thể phát giọng đọc: " + ex.Message, "OK"); }
            }
        }

        private void StopSpeech_Clicked(object sender, EventArgs e)
        {
            if (_speechCancelToken != null && !_speechCancelToken.IsCancellationRequested) _speechCancelToken.Cancel();
        }
    }

    public static class ColorExtensions
    {
        public static Mapsui.Styles.Color ToMapsui(this Microsoft.Maui.Graphics.Color c) =>
            new Mapsui.Styles.Color((int)(c.Red * 255), (int)(c.Green * 255), (int)(c.Blue * 255), (int)(c.Alpha * 255));
    }
}