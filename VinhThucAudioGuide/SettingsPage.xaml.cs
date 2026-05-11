using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using VinhThucAudioGuide.Services;

namespace VinhThucAudioGuide;

public partial class SettingsPage : ContentPage
{
    bool isProcessing = false;

    public SettingsPage()
    {
        InitializeComponent();
        LoadSettings(); // Tải lại cài đặt cũ khi mở app
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LocalizationManager.Instance.PropertyChanged += OnLocalizationChanged;
        // Mỗi lần quay lại Settings, refresh hiển thị pref hiện tại (vd: vừa đổi ở LanguageSelectionPage)
        UpdateUI();
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
            MainThread.BeginInvokeOnMainThread(UpdateUI);
        }
    }

    private async void OnUpdateClicked(object sender, EventArgs e)
    {
        var lm = LocalizationManager.Instance;
        if (Preferences.Default.Get("LastUpdateCheck", DateTime.MinValue.ToString()) is string s && DateTime.TryParse(s, out var last))
        {
            // no-op, just read
        }

        // Thực hiện kiểm tra cập nhật dữ liệu từ Web API (địa chỉ API cần cấu hình)
        try
        {
            BtnUpdate.IsEnabled = false;
            BtnUpdate.Text = lm.UpdatingButton;

            // Ví dụ gọi API -> /api/mobile/locations (tùy backend), ở đây ta dùng HttpClient cơ bản
            using var client = new System.Net.Http.HttpClient();
            var apiUrl = ApiService.GetConfiguredApiBase();
            var resp = await client.GetAsync($"{apiUrl.TrimEnd('/')}/api/mobile/locations");
            if (!resp.IsSuccessStatusCode)
            {
                await DisplayAlert(lm.ErrorTitle, lm.ServerFetchFailedMessage, lm.OkButton);
                return;
            }

            var json = await resp.Content.ReadAsStringAsync();
            var remoteList = System.Text.Json.JsonSerializer.Deserialize<List<Models.TourLocation>>(json);
            if (remoteList == null) { await DisplayAlert(lm.ErrorTitle, lm.EmptyServerDataMessage, lm.OkButton); return; }

            var db = new Services.LocalDbService();
            int added = await db.UpsertTourLocations(remoteList);

            Preferences.Default.Set("LastUpdateCheck", DateTime.UtcNow.ToString());
            BtnUpdate.Text = $"{lm.CheckUpdateButton} ({added})";
            await DisplayAlert(lm.UpdateTitle, lm.UpdateSuccessMessage(added), lm.OkButton);
        }
        catch (Exception ex)
        {
            await DisplayAlert(lm.ErrorTitle, ex.Message, lm.OkButton);
        }
        finally
        {
            BtnUpdate.IsEnabled = true;
            BtnUpdate.Text = lm.CheckUpdateButton;
        }
    }

    private void LoadSettings()
    {
        // Ưu tiên pref_language_code (chính xác hơn) — fallback "AppLang" để tương thích cũ.
        var prefCode = Preferences.Default.Get("pref_language_code", string.Empty);
        if (!string.IsNullOrEmpty(prefCode))
        {
            LocalizationManager.Instance.SetLanguageByCode(prefCode);
        }
        else
        {
            string lang = Preferences.Default.Get("AppLang", "Tiếng Việt");
            LocalizationManager.Instance.CurrentLanguage = lang;
        }

        bool auto = Preferences.Default.Get("AutoPlay", true);
        double speed = Preferences.Default.Get("VoiceSpeed", 1.0);
        SwAutoPlay.IsToggled = auto;
        SldSpeed.Value = speed;
        EntSpeed.Text = speed.ToString("F2");
        UpdateUI();
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        // Lưu các tuỳ chọn audio (auto play / tốc độ). Ngôn ngữ + giọng đọc được quản lý
        // ở flow LanguageSelectionPage → VoiceSelectionPage, không đụng tới ở đây để tránh
        // ghi đè preference user đã chọn.
        Preferences.Default.Set("AutoPlay", SwAutoPlay.IsToggled);
        Preferences.Default.Set("VoiceSpeed", SldSpeed.Value);

        // Gửi cấu hình mới lên server (heartbeat sẽ tự đọc pref_language_id để KHÔNG ghi đè ngôn ngữ).
        var apiService = IPlatformApplication.Current?.Services.GetService<ApiService>();
        if (apiService != null)
        {
            await apiService.SendHeartbeatAsync(autoPlay: SwAutoPlay.IsToggled, speechRate: SldSpeed.Value);
        }

        var lm = LocalizationManager.Instance;
        await DisplayAlert(lm.SuccessTitle, lm.SaveSuccessMessage, lm.OkButton);
    }

    private void OnSldSpeedChanged(object sender, ValueChangedEventArgs e)
    {
        if (isProcessing) return;
        isProcessing = true;
        EntSpeed.Text = e.NewValue.ToString("F2");
        isProcessing = false;
    }

    private void OnEntSpeedChanged(object sender, TextChangedEventArgs e)
    {
        if (isProcessing) return;
        if (double.TryParse(e.NewTextValue, out double val) && val >= 0.25 && val <= 2.0)
        {
            isProcessing = true;
            SldSpeed.Value = val;
            isProcessing = false;
        }
    }

    // Click vào hàng "Ngôn ngữ ứng dụng" → mở lại flow chọn ngôn ngữ + giọng đọc động từ server
    // (giống flow sau khi quét QR). Như vậy ngôn ngữ thuyết minh & UI luôn đồng bộ.
    private void OpenLangMenu(object sender, EventArgs e)
    {
        if (Application.Current != null)
            Application.Current.MainPage = new NavigationPage(new LanguageSelectionPage());
    }

    // CloseLangMenu/OnLangSelected vẫn được giữ trong XAML (để tương thích) nhưng KHÔNG còn dùng.
    // Có thể được gọi nếu user vô tình tap vào modal cũ — fallback an toàn về flow mới.
    private void CloseLangMenu(object sender, EventArgs e)
    {
        Overlay.IsVisible = false;
        LangModal.TranslationY = 600;
    }

    private void OnLangSelected(object sender, EventArgs e)
    {
        OpenLangMenu(sender, e);
    }

    private void UpdateUI()
    {
        var lm = LocalizationManager.Instance;
        Title = lm.TabSettings;
        LblHeader.Text = lm.SettingsTitle;
        BtnSave.Text = lm.SaveButton;
        BtnUpdate.Text = lm.CheckUpdateButton;
        LblLang.Text = lm.LanguageLabel;
        LblAuto.Text = lm.AutoPlayLabel;
        LblSpeed.Text = lm.VoiceSpeedLabel;
        LblChooseLanguage.Text = lm.ChooseLanguageTitle;
        BtnLangVi.Text = "🇻🇳 Tiếng Việt";
        BtnLangEn.Text = "🇬🇧 English";
        BtnLangFr.Text = "🇫🇷 Français";
        BtnLangZh.Text = "🇨🇳 中文";
        BtnLangKo.Text = "🇰🇷 한국어";

        GroupLang.Text = lm.GroupLanguage;
        GroupAudio.Text = lm.GroupAudio;

        // Lấy display + cờ từ pref keys (server-driven), không hardcode 5 ngôn ngữ.
        var prefDisplay = Preferences.Default.Get("pref_language_display_name", string.Empty);
        var prefCode = Preferences.Default.Get("pref_language_code", string.Empty);
        if (string.IsNullOrEmpty(prefDisplay))
            prefDisplay = lm.CurrentLanguage;
        var flag = LocalizationManager.GetFlagByCode(prefCode);
        LblCurrentLang.Text = $"{flag} {prefDisplay}".Trim();
    }
}