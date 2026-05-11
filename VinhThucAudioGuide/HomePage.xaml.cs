using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Storage;
using System.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using VinhThucAudioGuide.Services;

namespace VinhThucAudioGuide
{
    public partial class HomePage : ContentPage
    {
        public HomePage()
        {
            InitializeComponent();
            UpdateUI();
        }

        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();
            SetNativeBackground();
        }

        /// <summary>
        /// Đặt background trực tiếp vào Android native view - bypass hoàn toàn MAUI Shell/ContentPage rendering.
        /// Đây là cách chắc chắn nhất để hiển thị ảnh background khi trang nằm trong Shell.
        /// </summary>
        private void SetNativeBackground()
        {
#if ANDROID
            try
            {
                if (Handler?.PlatformView is Android.Views.View androidView)
                {
                    var context = Android.App.Application.Context;

                    // Thử load drawable "hinhnenapp" từ Android resource
                    var resId = context.Resources!.GetIdentifier(
                        "hinhnenapp", "drawable", context.PackageName);

                    if (resId != 0)
                    {
                        androidView.SetBackgroundResource(resId);
                        return;
                    }

                    // Fallback: đặt màu đen nếu không tìm thấy drawable
                    androidView.SetBackgroundColor(Android.Graphics.Color.Black);
                }
            }
            catch
            {
                // Ignore - không crash app nếu không set được background
            }
#endif
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LocalizationManager.Instance.PropertyChanged += OnLocalizationChanged;
            UpdateUI();
            // Gọi lại để đảm bảo background được set sau khi trang hoàn toàn hiển thị
            SetNativeBackground();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            LocalizationManager.Instance.PropertyChanged -= OnLocalizationChanged;
        }

        private void OnLocalizationChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName) ||
                e.PropertyName == nameof(LocalizationManager.CurrentLanguage))
            {
                MainThread.BeginInvokeOnMainThread(UpdateUI);
            }
        }

        private void UpdateUI()
        {
            var lm = LocalizationManager.Instance;
            Title = lm.TabHome;
            LblTitle.Text = lm.AppTitle;
            LblDesc.Text = lm.WelcomeText;
            BtnStart.Text = lm.StartButton;
        }

        private async void BtnGoToMap_Clicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//MainPage");
        }
    }

    // ============================================================================
    // LanguageSelectionPage - trang chon NGON NGU thuyet minh (buoc 1/2 sau quet QR)
    // UI duoc build bang C# (khong dung XAML) de tranh van de encoding cua tool ghi file.
    // ============================================================================
    public class LanguageSelectionPage : ContentPage
    {
        private readonly ObservableCollection<LanguageItem> _items = new();
        private bool _isNavigating;

        // Cac control duoc giu reference de Load/Show/Hide trong code.
        private readonly ActivityIndicator LoadingIndicator;
        private readonly CollectionView LanguagesView;
        private readonly Button BtnRetry;

        public LanguageSelectionPage()
        {
            Title = "Chon ngon ngu";
            BackgroundColor = Color.FromArgb("#0F1115");
            NavigationPage.SetHasBackButton(this, false);

            var lblHeader = new Label
            {
                Text = "Chon ngon ngu thuyet minh",
                FontSize = 22,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                HorizontalTextAlignment = TextAlignment.Center
            };

            var lblSubtitle = new Label
            {
                Text = "Hay chon ngon ngu ban muon nghe. Buoc sau ban se chon giong doc.",
                FontSize = 13,
                TextColor = Color.FromArgb("#B8B8B8"),
                LineHeight = 1.4,
                HorizontalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(6, 0, 6, 8)
            };

            LoadingIndicator = new ActivityIndicator
            {
                IsRunning = true,
                IsVisible = true,
                Color = Color.FromArgb("#FFCC00"),
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center
            };

            LanguagesView = new CollectionView
            {
                IsVisible = false,
                SelectionMode = SelectionMode.None,
                ItemsSource = _items,
                ItemTemplate = new DataTemplate(() => BuildLanguageItemTemplate()),
                EmptyView = new VerticalStackLayout
                {
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    Spacing = 8,
                    Children =
                    {
                        new Label { Text = "Khong tai duoc danh sach ngon ngu.", TextColor = Colors.White, HorizontalTextAlignment = TextAlignment.Center },
                        new Label { Text = "Kiem tra ket noi mang va thu lai.", FontSize = 12, TextColor = Color.FromArgb("#9AA0A6"), HorizontalTextAlignment = TextAlignment.Center }
                    }
                }
            };

            BtnRetry = new Button
            {
                Text = "Tai lai",
                IsVisible = false,
                BackgroundColor = Color.FromArgb("#FFCC00"),
                TextColor = Color.FromArgb("#111111"),
                FontAttributes = FontAttributes.Bold,
                CornerRadius = 24,
                HeightRequest = 48
            };
            BtnRetry.Clicked += OnRetryClicked;

            var grid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition(GridLength.Auto),
                    new RowDefinition(GridLength.Auto),
                    new RowDefinition(GridLength.Star),
                    new RowDefinition(GridLength.Auto)
                },
                Padding = new Thickness(20, 28, 20, 24),
                RowSpacing = 14
            };
            grid.Add(lblHeader, 0, 0);
            grid.Add(lblSubtitle, 0, 1);
            grid.Add(LoadingIndicator, 0, 2);
            grid.Add(LanguagesView, 0, 2);
            grid.Add(BtnRetry, 0, 3);

            Content = grid;
        }

        // Build template cho moi dong ngon ngu trong CollectionView.
        // Day la instance method (khong static) de bat duoc 'this' page cho TapGestureRecognizer.
        private Border BuildLanguageItemTemplate()
        {
            var border = new Border
            {
                BackgroundColor = Color.FromArgb("#1B1F26"),
                Stroke = new SolidColorBrush(Color.FromArgb("#2C313A")),
                StrokeThickness = 1,
                Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(16, 14),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 }
            };

            var lblFlag = new Label { FontSize = 32, VerticalOptions = LayoutOptions.Center };
            lblFlag.SetBinding(Label.TextProperty, nameof(LanguageItem.FlagEmoji));

            var lblName = new Label { FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Colors.White };
            lblName.SetBinding(Label.TextProperty, nameof(LanguageItem.DisplayName));

            var lblSubtitle = new Label { FontSize = 12, TextColor = Color.FromArgb("#9AA0A6") };
            lblSubtitle.SetBinding(Label.TextProperty, nameof(LanguageItem.Subtitle));

            var stackText = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
            stackText.Add(lblName);
            stackText.Add(lblSubtitle);

            var lblArrow = new Label { Text = ">", FontSize = 24, TextColor = Color.FromArgb("#FFCC00"), VerticalOptions = LayoutOptions.Center };

            var innerGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto)
                },
                ColumnSpacing = 14,
                VerticalOptions = LayoutOptions.Center
            };
            innerGrid.Add(lblFlag, 0, 0);
            innerGrid.Add(stackText, 1, 0);
            innerGrid.Add(lblArrow, 2, 0);

            border.Content = innerGrid;

            var tap = new TapGestureRecognizer();
            tap.Tapped += async (sender, e) =>
            {
                if (sender is BindableObject bo && bo.BindingContext is LanguageItem item)
                {
                    await NavigateToVoiceAsync(item);
                }
            };
            border.GestureRecognizers.Add(tap);

            return border;
        }

        // Goi tu TapGestureRecognizer cua tung dong. Push sang VoiceSelectionPage.
        internal async Task NavigateToVoiceAsync(LanguageItem item)
        {
            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                await Navigation.PushAsync(new VoiceSelectionPage(item));
            }
            finally
            {
                _isNavigating = false;
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            _isNavigating = false;
            await LoadLanguagesAsync();
        }

        // Tai danh sach ngon ngu active tu /languages/active, fallback local SQLite neu offline.
        private async Task LoadLanguagesAsync()
        {
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;
            LanguagesView.IsVisible = false;
            BtnRetry.IsVisible = false;
            _items.Clear();

            var apiService = IPlatformApplication.Current?.Services.GetService<ApiService>();
            List<ActiveLanguage>? remote = null;

            if (apiService != null)
            {
                try { remote = await apiService.GetActiveLanguagesAsync(); }
                catch { }
            }

            if (remote != null && remote.Count > 0)
            {
                foreach (var lang in remote)
                {
                    _items.Add(new LanguageItem
                    {
                        Id          = lang.Id,
                        Code        = lang.Code,
                        Name        = lang.Name,
                        DisplayName = string.IsNullOrWhiteSpace(lang.DisplayName) ? lang.Name : lang.DisplayName,
                        FlagCode    = lang.FlagCode,
                        FlagEmoji   = BuildFlagEmoji(lang.FlagCode),
                        Subtitle    = string.IsNullOrWhiteSpace(lang.Code) ? lang.Name : lang.Code.ToUpperInvariant()
                    });
                }
            }
            else
            {
                try
                {
                    var localDb = new LocalDbService();
                    var localLangs = await localDb.GetAllLanguages();
                    foreach (var l in localLangs)
                    {
                        _items.Add(new LanguageItem
                        {
                            Id          = l.ServerId,
                            Code        = l.LangCode,
                            Name        = l.LangName,
                            DisplayName = l.LangName,
                            FlagCode    = l.FlagCode,
                            FlagEmoji   = BuildFlagEmoji(l.FlagCode),
                            Subtitle    = string.IsNullOrWhiteSpace(l.LangCode) ? l.LangName : l.LangCode.ToUpperInvariant()
                        });
                    }
                }
                catch { }
            }

            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
            LanguagesView.IsVisible = true;
            BtnRetry.IsVisible = _items.Count == 0;
        }

        private async void OnRetryClicked(object? sender, EventArgs e) => await LoadLanguagesAsync();

        // Chuyen ISO-2 (vn/us/fr) -> emoji co bang regional indicator symbols U+1F1E6..
        private static string BuildFlagEmoji(string? flagCode)
        {
            if (string.IsNullOrWhiteSpace(flagCode) || flagCode.Length < 2) return string.Empty;
            char c1 = char.ToUpperInvariant(flagCode[0]);
            char c2 = char.ToUpperInvariant(flagCode[1]);
            if (c1 < 'A' || c1 > 'Z' || c2 < 'A' || c2 > 'Z') return string.Empty;
            int code1 = 0x1F1E6 + (c1 - 'A');
            int code2 = 0x1F1E6 + (c2 - 'A');
            return char.ConvertFromUtf32(code1) + char.ConvertFromUtf32(code2);
        }

        // DTO hien thi tren CollectionView, dong thoi mang du lieu sang VoiceSelectionPage.
        public class LanguageItem
        {
            public string Id { get; set; } = string.Empty;
            public string Code { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public string? FlagCode { get; set; }
            public string FlagEmoji { get; set; } = string.Empty;
            public string Subtitle { get; set; } = string.Empty;
        }
    }

    // ============================================================================
    // VoiceSelectionPage - trang chon GIONG DOC (buoc 2/2). Nhan language tu buoc truoc.
    // Sau khi user chon giong -> POST /api/device-preference + luu Preferences -> AppShell.
    // UI duoc build bang C# (khong dung XAML).
    // ============================================================================
    public class VoiceSelectionPage : ContentPage
    {
        private readonly LanguageSelectionPage.LanguageItem _language;
        private readonly ObservableCollection<VoiceItem> _items = new();
        private bool _isSaving;

        private readonly Label LblLangFlag;
        private readonly Label LblLangName;
        private readonly ActivityIndicator LoadingIndicator;
        private readonly CollectionView VoicesView;
        private readonly Button BtnSkip;
        private readonly Grid SavingOverlay;

        public VoiceSelectionPage(LanguageSelectionPage.LanguageItem language)
        {
            _language = language ?? throw new ArgumentNullException(nameof(language));

            Title = "Chon giong doc";
            BackgroundColor = Color.FromArgb("#0F1115");

            var lblHeader = new Label
            {
                Text = "Chon giong doc",
                FontSize = 22,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                HorizontalTextAlignment = TextAlignment.Center
            };

            LblLangFlag = new Label
            {
                FontSize = 20,
                Text = string.IsNullOrEmpty(_language.FlagEmoji) ? "\U0001F310" : _language.FlagEmoji
            };
            LblLangName = new Label
            {
                FontSize = 14,
                TextColor = Color.FromArgb("#E8E8E8"),
                VerticalOptions = LayoutOptions.Center,
                Text = string.IsNullOrWhiteSpace(_language.DisplayName) ? _language.Name : _language.DisplayName
            };

            var ribbonStack = new HorizontalStackLayout
            {
                Spacing = 8,
                VerticalOptions = LayoutOptions.Center,
                Children = { LblLangFlag, LblLangName }
            };
            var ribbon = new Border
            {
                BackgroundColor = Color.FromArgb("#1B1F26"),
                Stroke = new SolidColorBrush(Color.FromArgb("#2C313A")),
                StrokeThickness = 1,
                Padding = new Thickness(14, 10),
                Margin = new Thickness(0, 4, 0, 8),
                HorizontalOptions = LayoutOptions.Center,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 20 },
                Content = ribbonStack
            };

            LoadingIndicator = new ActivityIndicator
            {
                IsRunning = true,
                IsVisible = true,
                Color = Color.FromArgb("#FFCC00"),
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center
            };

            VoicesView = new CollectionView
            {
                IsVisible = false,
                SelectionMode = SelectionMode.None,
                ItemsSource = _items,
                ItemTemplate = new DataTemplate(() => BuildVoiceItemTemplate()),
                EmptyView = new VerticalStackLayout
                {
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    Spacing = 8,
                    Children =
                    {
                        new Label { Text = "Chua co giong doc cho ngon ngu nay.", TextColor = Colors.White, HorizontalTextAlignment = TextAlignment.Center },
                        new Label { Text = "Bam Bo qua de dung giong mac dinh.", FontSize = 12, TextColor = Color.FromArgb("#9AA0A6"), HorizontalTextAlignment = TextAlignment.Center }
                    }
                }
            };

            BtnSkip = new Button
            {
                Text = "Bo qua, dung giong mac dinh",
                BackgroundColor = Colors.Transparent,
                TextColor = Color.FromArgb("#FFCC00"),
                BorderColor = Color.FromArgb("#FFCC00"),
                BorderWidth = 1,
                CornerRadius = 24,
                HeightRequest = 48,
                Margin = new Thickness(0, 4, 0, 0)
            };
            BtnSkip.Clicked += OnSkipClicked;

            SavingOverlay = new Grid
            {
                IsVisible = false,
                BackgroundColor = Color.FromArgb("#CC000000"),
                Children =
                {
                    new VerticalStackLayout
                    {
                        VerticalOptions = LayoutOptions.Center,
                        HorizontalOptions = LayoutOptions.Center,
                        Spacing = 12,
                        Children =
                        {
                            new ActivityIndicator { IsRunning = true, Color = Color.FromArgb("#FFCC00"), HeightRequest = 48 },
                            new Label { Text = "Dang luu lua chon...", TextColor = Colors.White, FontSize = 14 }
                        }
                    }
                }
            };

            var grid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition(GridLength.Auto),
                    new RowDefinition(GridLength.Auto),
                    new RowDefinition(GridLength.Star),
                    new RowDefinition(GridLength.Auto)
                },
                Padding = new Thickness(20, 24, 20, 24),
                RowSpacing = 12
            };
            grid.Add(lblHeader, 0, 0);
            grid.Add(ribbon, 0, 1);
            grid.Add(LoadingIndicator, 0, 2);
            grid.Add(VoicesView, 0, 2);
            grid.Add(BtnSkip, 0, 3);
            // Overlay phu toan bo grid: span 4 row.
            Grid.SetRowSpan(SavingOverlay, 4);
            grid.Add(SavingOverlay, 0, 0);

            Content = grid;
        }

        // Template moi dong giong doc trong CollectionView.
        private Border BuildVoiceItemTemplate()
        {
            var border = new Border
            {
                BackgroundColor = Color.FromArgb("#1B1F26"),
                Stroke = new SolidColorBrush(Color.FromArgb("#2C313A")),
                StrokeThickness = 1,
                Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(16, 14),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 }
            };

            var lblIcon = new Label { Text = "\U0001F399", FontSize = 22, VerticalOptions = LayoutOptions.Center };

            var lblName = new Label { FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Colors.White };
            lblName.SetBinding(Label.TextProperty, nameof(VoiceItem.DisplayName));

            var lblSubtitle = new Label { FontSize = 12, TextColor = Color.FromArgb("#9AA0A6") };
            lblSubtitle.SetBinding(Label.TextProperty, nameof(VoiceItem.Subtitle));

            var stackText = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
            stackText.Add(lblName);
            stackText.Add(lblSubtitle);

            var lblArrow = new Label { Text = ">", FontSize = 24, TextColor = Color.FromArgb("#FFCC00"), VerticalOptions = LayoutOptions.Center };

            var innerGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto)
                },
                ColumnSpacing = 14,
                VerticalOptions = LayoutOptions.Center
            };
            innerGrid.Add(lblIcon, 0, 0);
            innerGrid.Add(stackText, 1, 0);
            innerGrid.Add(lblArrow, 2, 0);

            border.Content = innerGrid;

            var tap = new TapGestureRecognizer();
            tap.Tapped += async (sender, e) =>
            {
                if (sender is BindableObject bo && bo.BindingContext is VoiceItem item)
                {
                    await SaveAndEnterAppAsync(item.Id, item.DisplayName);
                }
            };
            border.GestureRecognizers.Add(tap);

            return border;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            _isSaving = false;
            await LoadVoicesAsync();
        }

        private async Task LoadVoicesAsync()
        {
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;
            VoicesView.IsVisible = false;
            BtnSkip.IsVisible = false;
            _items.Clear();

            var apiService = IPlatformApplication.Current?.Services.GetService<ApiService>();
            if (apiService != null && !string.IsNullOrEmpty(_language.Id))
            {
                try
                {
                    var voices = await apiService.GetVoicesAsync(_language.Id);
                    if (voices != null)
                    {
                        foreach (var v in voices)
                        {
                            _items.Add(new VoiceItem
                            {
                                Id          = v.Id,
                                DisplayName = v.DisplayName,
                                IsDefault   = v.IsDefault,
                                Subtitle    = v.IsDefault ? "Mac dinh" : "Giong doc"
                            });
                        }
                    }
                }
                catch { }
            }

            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
            VoicesView.IsVisible = true;
            BtnSkip.IsVisible = true;
        }

        private async void OnSkipClicked(object? sender, EventArgs e)
        {
            if (_isSaving) return;
            await SaveAndEnterAppAsync(null, null);
        }

        // Luu pref + POST /api/device-preference + chuyen vao AppShell.
        // 6 key pref_* sau day duoc dung o:
        //   - MainPage.Speak_Clicked: bo qua action sheet, dung pref_language_code + pref_voice_id
        //     de goi /api/mobile/narration truc tiep.
        //   - LocalizationManager.CurrentLanguage: gan = pref_language_display_name de UI dich theo.
        //   - SplashPage: kiem tra pref_language_id de quyet dinh vao thang AppShell hay chay flow chon.
        private async Task SaveAndEnterAppAsync(Guid? voiceId, string? voiceDisplayName)
        {
            _isSaving = true;
            SavingOverlay.IsVisible = true;

            try
            {
                Preferences.Default.Set("pref_language_id",           _language.Id ?? string.Empty);
                Preferences.Default.Set("pref_language_code",         _language.Code ?? string.Empty);
                Preferences.Default.Set("pref_language_name",         _language.Name ?? string.Empty);
                Preferences.Default.Set("pref_language_display_name", _language.DisplayName ?? string.Empty);
                Preferences.Default.Set("pref_voice_id",              voiceId?.ToString() ?? string.Empty);
                Preferences.Default.Set("pref_voice_display_name",    voiceDisplayName ?? string.Empty);

                // Đổi ngôn ngữ UI theo code chuẩn (vi/en/fr/zh/ko) để map đúng với LocalizationManager,
                // tránh trường hợp DisplayName từ server ("Vietnamese") không khớp switch ("Tiếng Việt").
                if (!string.IsNullOrWhiteSpace(_language.Code))
                {
                    LocalizationManager.Instance.SetLanguageByCode(_language.Code);
                    Preferences.Default.Set("AppLang", LocalizationManager.Instance.CurrentLanguage);
                }

                var apiService = IPlatformApplication.Current?.Services.GetService<ApiService>();
                if (apiService != null && !string.IsNullOrEmpty(_language.Id))
                {
                    _ = await apiService.SaveDevicePreferenceAsync(_language.Id, voiceId);
                }
            }
            finally
            {
                SavingOverlay.IsVisible = false;
                _isSaving = false;
            }

            if (Application.Current != null)
                Application.Current.MainPage = new AppShell();
        }

        public class VoiceItem
        {
            public Guid Id { get; set; }
            public string DisplayName { get; set; } = string.Empty;
            public bool IsDefault { get; set; }
            public string Subtitle { get; set; } = string.Empty;
        }
    }
}