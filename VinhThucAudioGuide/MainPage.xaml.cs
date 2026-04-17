using Mapsui;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
using Mapsui.Projections;
using Mapsui.Layers;
using Mapsui.Styles;
using Mapsui.Providers;
using Plugin.Maui.Audio;
using System.Net.Http;
using System.Text.Json;
using Color = Microsoft.Maui.Graphics.Color;
using Microsoft.Maui.ApplicationModel;
using System;
using System.Linq;
using System.Threading;
using Microsoft.Maui.Controls;           // Trị lỗi ContentPage, Button, SelectionChangedEventArgs
using Microsoft.Maui.Graphics;           // Trị lỗi Colors
using Microsoft.Maui.Devices.Sensors;    // Trị lỗi Location, Geolocation, GeolocationRequest
using Microsoft.Maui.Storage;            // Dùng để đọc Preferences (Sổ tay cài đặt ngôn ngữ)
using Microsoft.Maui.Media;

namespace VinhThucAudioGuide
{
    public class POI
    {
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
            LoadData();
            GetLocationAndCenter();
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

        private void LoadData()
        {
            _allPois = new List<POI>
            {
                new POI {
                    Name = "Bánh mì Huỳnh Hoa", Category = "Ẩm thực",
                    Latitude = 10.7716, Longitude = 106.6923,
                    ImageUrl = "https://dynamic-media-cdn.tripadvisor.com/media/photo-o/19/66/94/19/banh-mi-362.jpg?w=900&h=-1&s=1",
                    Rating = 4.8, PinColor = Colors.Orange,
                    AudioScripts = new Dictionary<string, string> {
                        { "vi", "Bánh mì Huỳnh Hoa là tiệm bánh mì nổi tiếng tại Sài Gòn, được yêu thích nhờ ổ bánh to, nhiều nhân và hương vị đậm đà. Với công thức đặc trưng cùng lịch sử lâu năm, nơi đây trở thành điểm đến quen thuộc của cả người dân lẫn du khách." },
                        { "th", "บั๋นหมี่ ฮวี่นฮวา เป็นร้านแซนด์วิชที่มีชื่อเสียงในไซง่อน เป็นที่ชื่นชอบเพราะขนมปังก้อนใหญ่ ไส้เยอะ และรสชาติเข้มข้น ด้วยสูตรเฉพาะและประวัติศาสตร์อันยาวนาน ที่นี่จึงกลายเป็นจุดหมายปลายทางยอดนิยมสำหรับทั้งคนในพื้นที่และนักท่องเที่ยว" },
                        { "en-US", "Huynh Hoa Banh Mi is a famous sandwich shop in Saigon, loved for its large size, generous fillings, and rich flavor. With its signature recipe and long history, it has become a familiar destination for both locals and tourists." },
                        { "en-AU", "Huynh Hoa Banh Mi is a famous sandwich shop in Saigon, loved for its large size, generous fillings, and rich flavor. With its signature recipe and long history, it has become a familiar destination for both locals and tourists, mate." },
                        { "en-CA", "Huynh Hoa Banh Mi is a famous sandwich shop in Saigon, loved for its large size, generous fillings, and rich flavor. With its signature recipe and long history, it has become a familiar destination for both locals and tourists, eh." }
                    }
                },
                new POI {
                    Name = "Bảo tàng Lịch sử TP.HCM", Category = "Du lịch",
                    Latitude = 10.7882, Longitude = 106.7049,
                    ImageUrl = "https://images.unsplash.com/photo-1596422846543-75c6fc197f07?w=500",
                    Rating = 4.5, PinColor = Colors.Blue,
                    AudioScripts = new Dictionary<string, string> {
                        { "vi", "Bảo tàng Lịch sử Việt Nam là nơi lưu giữ nhiều hiện vật quý giá, tái hiện quá trình hình thành và phát triển của dân tộc. Với không gian trưng bày phong phú, đây là điểm tham quan hấp dẫn cho những ai yêu thích lịch sử và văn hóa." },
                        { "th", "พิพิธภัณฑ์ประวัติศาสตร์เวียดนามเป็นสถานที่เก็บรวบรวมโบราณวัตถุอันล้ำค่ามากมาย ซึ่งจำลองกระบวนการก่อตั้งและพัฒนาของชาติ ด้วยพื้นที่จัดแสดงที่หลากหลาย ที่นี่จึงเป็นจุดหมายปลายทางที่น่าสนใจสำหรับผู้ที่รักประวัติศาสตร์และวัฒนธรรม" },
                        { "en-US", "The Vietnam History Museum houses many precious artifacts, recreating the nation's formation and development process. With its diverse exhibition spaces, it is an attractive destination for those who love history and culture." },
                        { "en-AU", "The Vietnam History Museum houses many precious artifacts, recreating the nation's formation and development process. It is an attractive destination for those who love history and culture." },
                        { "en-CA", "The Vietnam History Museum houses many precious artifacts, recreating the nation's formation and development process. It is an attractive destination for those who love history and culture." }
                    }
                },
                new POI {
                    Name = "Lễ hội Ẩm thực Đêm", Category = "Sự kiện",
                    Latitude = 10.7745, Longitude = 106.7001,
                    ImageUrl = "https://images.unsplash.com/photo-1555939594-58d7cb561ad1?w=500",
                    Rating = 5.0, PinColor = Colors.Purple,
                    AudioScripts = new Dictionary<string, string> {
                        { "vi", "Lễ hội Ẩm thực Đêm Vũng Tàu là sự kiện thu hút đông đảo du khách với nhiều gian hàng ăn uống phong phú. Tại đây, du khách có thể thưởng thức các món hải sản tươi ngon và đặc sản địa phương trong không khí sôi động về đêm." },
                        { "th", "เทศกาลอาหารกลางคืนวุงเตาเป็นเหตุการณ์ที่ดึงดูดนักท่องเที่ยวจำนวนมากด้วยแผงขายอาหารที่หลากหลาย ที่นี่นักท่องเที่ยวสามารถเพลิดเพลินกับอาหารทะเลสดใหม่และอาหารพื้นเมืองในบรรยากาศยามค่ำคืนที่คึกคัก" },
                        { "en-US", "The Vung Tau Night Food Festival is an event that attracts a large number of tourists with many diverse food stalls. Here, visitors can enjoy fresh seafood and local specialties in a vibrant nighttime atmosphere." },
                        { "en-AU", "The Vung Tau Night Food Festival is an event that attracts a large number of tourists with many diverse food stalls. You can enjoy fresh seafood and local specialties in a vibrant nighttime atmosphere, mate." },
                        { "en-CA", "The Vung Tau Night Food Festival is an event that attracts a large number of tourists with many diverse food stalls. You can enjoy fresh seafood and local specialties in a vibrant nighttime atmosphere, eh." }
                    }
                }
            };
            cvPoiList.ItemsSource = _allPois;
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
            string category = btn.Text.Contains("Ẩm thực") ? "Ẩm thực" : btn.Text.Contains("Du lịch") ? "Du lịch" : "Sự kiện";
            cvPoiList.ItemsSource = _allPois.Where(p => p.Category == category).ToList();
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

            string action = await DisplayActionSheet("Ngôn ngữ", "Hủy", null, "🇻🇳 Tiếng Việt", "🇹🇭 Tiếng Thái", "🇺🇸 Tiếng Anh (Mỹ)", "🇦🇺 Tiếng Anh (Úc)", "🇨🇦 Tiếng Anh (Canada)");
            if (action == "Hủy" || string.IsNullOrEmpty(action)) return;

            string text = "", lang = "vi";
            if (action.Contains("Việt")) { text = selectedPoi.AudioScripts.GetValueOrDefault("vi", selectedPoi.Name); lang = "vi"; }
            else if (action.Contains("Thái")) { text = selectedPoi.AudioScripts.GetValueOrDefault("th", selectedPoi.Name); lang = "th"; }
            else if (action.Contains("Mỹ")) { text = selectedPoi.AudioScripts.GetValueOrDefault("en-US", selectedPoi.Name); lang = "en-US"; }
            else if (action.Contains("Úc")) { text = selectedPoi.AudioScripts.GetValueOrDefault("en-AU", selectedPoi.Name); lang = "en-AU"; }
            else if (action.Contains("Canada")) { text = selectedPoi.AudioScripts.GetValueOrDefault("en-CA", selectedPoi.Name); lang = "en-CA"; }

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

                foreach (var sentence in finalSentences)
                {
                    if (currentId != _speechId) break;

                    string cleanSentence = sentence.Trim();
                    if (string.IsNullOrEmpty(cleanSentence)) continue;

                    string url = $"https://translate.google.com/translate_tts?ie=UTF-8&q={Uri.EscapeDataString(cleanSentence)}&tl={lang}&client=tw-ob";
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
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi âm thanh", "Không tải được tiếng! Chi tiết: " + ex.Message, "OK");
            }
        }

        private void StopSpeech_Clicked(object sender, EventArgs e)
        {
            _speechId++;
            if (_currentAudioPlayer != null && _currentAudioPlayer.IsPlaying) _currentAudioPlayer.Stop();
        }
    }
}