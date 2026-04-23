using Android.Speech.Tts;
using Microsoft.Maui.Controls;
using System;

[assembly: Dependency(typeof(VinhThucAudioGuide.Platforms.Android.TextToSpeechService))]
namespace VinhThucAudioGuide.Platforms.Android
{
    public class TextToSpeechService : Java.Lang.Object, VinhThucAudioGuide.Services.IPlatformTts, global::Android.Speech.Tts.TextToSpeech.IOnInitListener
    {
        global::Android.Speech.Tts.TextToSpeech _tts;
        string _pending;

        public void Speak(string text, string locale = null)
        {
            if (_tts == null)
            {
                _pending = text;
                _tts = new global::Android.Speech.Tts.TextToSpeech(global::Android.App.Application.Context, this);
                return;
            }

            if (!string.IsNullOrEmpty(locale))
            {
                try
                {
                    var parts = locale.Split('-');
                    var loc = parts.Length == 2 ? new global::Java.Util.Locale(parts[0], parts[1]) : new global::Java.Util.Locale(locale);
                    _tts.SetLanguage(loc);
                }
                catch { }
            }

            _tts.Speak(text, QueueMode.Flush, null, null);
        }

        public void OnInit(global::Android.Speech.Tts.OperationResult status)
        {
            if (status == global::Android.Speech.Tts.OperationResult.Success && !string.IsNullOrEmpty(_pending))
            {
                _tts.Speak(_pending, QueueMode.Flush, null, null);
                _pending = null;
            }
        }
    }
}
