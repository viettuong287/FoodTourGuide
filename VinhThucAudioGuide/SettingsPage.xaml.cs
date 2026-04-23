using Microsoft.Maui;           // Trị lỗi Easing
using Microsoft.Maui.Controls;  // Trị lỗi ContentPage, BoxView, Slider
using Microsoft.Maui.Storage;   // Trị lỗi Preferences
using System;

namespace VinhThucAudioGuide;

public partial class SettingsPage : ContentPage
{
    bool isProcessing = false;

    public SettingsPage()
    {
        InitializeComponent();
        LoadSettings(); // Tải lại cài đặt cũ khi mở app
    }

    private async void OnUpdateClicked(object sender, EventArgs e)
    {
        if (Preferences.Default.Get("LastUpdateCheck", DateTime.MinValue.ToString()) is string s && DateTime.TryParse(s, out var last))
        {
            // no-op, just read
        }

        // Thực hiện kiểm tra cập nhật dữ liệu từ Web API (địa chỉ API cần cấu hình)
        try
        {
            BtnUpdate.IsEnabled = false;
            BtnUpdate.Text = "Đang kiểm tra...";

            // Ví dụ gọi API -> /api/mobile/locations (tùy backend), ở đây ta dùng HttpClient cơ bản
            using var client = new System.Net.Http.HttpClient();
            var apiUrl = Preferences.Default.Get("RemoteApiBase", "https://your-web-api.example.com");
            var resp = await client.GetAsync($"{apiUrl.TrimEnd('/')}/api/mobile/locations");
            if (!resp.IsSuccessStatusCode)
            {
                await DisplayAlert("Lỗi", "Không lấy được dữ liệu từ server.", "OK");
                return;
            }

            var json = await resp.Content.ReadAsStringAsync();
            var remoteList = System.Text.Json.JsonSerializer.Deserialize<List<Models.TourLocation>>(json);
            if (remoteList == null) { await DisplayAlert("Lỗi", "Dữ liệu server rỗng.", "OK"); return; }

            var db = new Services.LocalDbService();
            int added = await db.UpsertTourLocations(remoteList);

            Preferences.Default.Set("LastUpdateCheck", DateTime.UtcNow.ToString());
            // Update button text to show number of new items
            BtnUpdate.Text = LocalizationManager.Instance.CurrentLanguage == "Tiếng Việt" ? $"Cập nhật mới ({added})" : $"Check for updates ({added})";
            await DisplayAlert("Cập nhật", $"Đã đồng bộ xong. Thêm {added} địa điểm mới.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", ex.Message, "OK");
        }
        finally
        {
            BtnUpdate.IsEnabled = true;
            BtnUpdate.Text = LocalizationManager.Instance.CurrentLanguage == "Tiếng Việt" ? "Cập nhật mới" : "Check for updates";
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

    private void OnSaveClicked(object sender, EventArgs e)
    {
        Preferences.Default.Set("AppLang", LocalizationManager.Instance.CurrentLanguage);
        Preferences.Default.Set("AutoPlay", SwAutoPlay.IsToggled);
        Preferences.Default.Set("VoiceSpeed", SldSpeed.Value);

        string msg = LocalizationManager.Instance.CurrentLanguage == "Tiếng Việt" ? "Lưu cài đặt thành công!" : "Saved successfully!";
        DisplayAlert("Thành công", msg, "OK");
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
        LblHeader.Text = lm.SettingsTitle;
        BtnSave.Text = lm.SaveButton;
        LblLang.Text = lm.LanguageLabel;
        LblAuto.Text = lm.AutoPlayLabel;
        LblSpeed.Text = lm.VoiceSpeedLabel;

        // Thêm 2 dòng này để dịch tiêu đề nhóm
        GroupLang.Text = lm.CurrentLanguage == "Tiếng Việt" ? "NGÔN NGỮ" : "LANGUAGE";
        GroupAudio.Text = lm.CurrentLanguage == "Tiếng Việt" ? "ÂM THANH & THUYẾT MINH" : "AUDIO & NARRATION";
    }
}