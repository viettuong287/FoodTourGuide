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
    private List<ActiveLanguage> _serverLanguages = [];

    public SettingsPage()
    {
        InitializeComponent();
        LoadSettings(); // Tải lại cài đặt cũ khi mở app
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        LocalizationManager.Instance.PropertyChanged += OnLocalizationChanged;
        UpdateUI();
        await LoadLanguagesFromServerAsync();
    }

    private async Task LoadLanguagesFromServerAsync()
    {
        var apiService = IPlatformApplication.Current?.Services.GetService<ApiService>();
        if (apiService != null)
        {
            var langs = await apiService.GetActiveLanguagesAsync();
            if (langs != null && langs.Count > 0)
                _serverLanguages = langs;
        }
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
        string lang = Preferences.Default.Get("AppLang", "Tiếng Việt");
        bool auto = Preferences.Default.Get("AutoPlay", true);
        double speed = Preferences.Default.Get("VoiceSpeed", 1.0);

        LocalizationManager.Instance.CurrentLanguage = lang;
        SwAutoPlay.IsToggled = auto;
        SldSpeed.Value = speed;
        EntSpeed.Text = speed.ToString("F2");
        UpdateUI();
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        Preferences.Default.Set("AppLang", LocalizationManager.Instance.CurrentLanguage);
        Preferences.Default.Set("AutoPlay", SwAutoPlay.IsToggled);
        Preferences.Default.Set("VoiceSpeed", SldSpeed.Value);

        // Gửi cấu hình lên Server với languageId từ API
        var apiService = IPlatformApplication.Current?.Services.GetService<ApiService>();
        if (apiService != null)
        {
            string langCode = LocalizationManager.Instance.CurrentLanguage switch
            {
                "English" => "en",
                "Français" => "fr",
                "中文" => "zh",
                "한국어" => "ko",
                _ => "vi"
            };
            var serverLang = _serverLanguages.FirstOrDefault(l => l.Code == langCode);
            string languageId = serverLang?.Id ?? Guid.Empty.ToString();
            await apiService.SendHeartbeatAsync(languageId, SwAutoPlay.IsToggled, SldSpeed.Value);
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

    private async void OpenLangMenu(object sender, EventArgs e)
    {
        Overlay.IsVisible = true;
        _ = Overlay.FadeTo(0.5);
        await LangModal.TranslateTo(0, 0, 300, Easing.SpringOut);
    }

    private async void CloseLangMenu(object sender, EventArgs e)
    {
        _ = Overlay.FadeTo(0);
        await LangModal.TranslateTo(0, 600, 300, Easing.SpringIn);
        Overlay.IsVisible = false;
    }

    private void OnLangSelected(object sender, EventArgs e)
    {
        var btn = (Button)sender;
        string selectedFull = btn.Text; // VD: "🇻🇳 Tiếng Việt"
        // Hỗ trợ emoji có thể là 2 codepoints, chuỗi bắt đầu bằng cờ + space
        string selectedName = selectedFull.Contains(" ") ? selectedFull.Substring(selectedFull.IndexOf(' ')).Trim() : selectedFull;

        // Map display names to internal language keys used by LocalizationManager
        string langKey = selectedName switch
        {
            "Tiếng Việt" => "Tiếng Việt",
            "English" => "English",
            "Français" => "Français",
            "中文" => "中文",
            "한국어" => "한국어",
            _ => selectedName
        };

        LocalizationManager.Instance.CurrentLanguage = langKey;
        LblCurrentLang.Text = selectedFull; // Gắn luôn cờ lên giao diện
        UpdateUI();
        CloseLangMenu(null, null);
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
        LblCurrentLang.Text = GetLanguageDisplayWithFlag(lm.CurrentLanguage);
    }

    private static string GetLanguageDisplayWithFlag(string langKey) => langKey switch
    {
        "English" => "🇬🇧 English",
        "Français" => "🇫🇷 Français",
        "中文" => "🇨🇳 中文",
        "한국어" => "🇰🇷 한국어",
        _ => "🇻🇳 Tiếng Việt"
    };
}