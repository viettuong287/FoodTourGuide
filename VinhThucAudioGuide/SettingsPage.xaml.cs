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
        string selectedName = selectedFull.Substring(3).Trim(); // Cắt lấy "Tiếng Việt"

        LocalizationManager.Instance.CurrentLanguage = selectedName;
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