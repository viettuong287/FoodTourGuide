using AVFoundation;
using Foundation;
using Microsoft.Maui.Controls;
using VinhThucAudioGuide.Services;

[assembly: Dependency(typeof(VinhThucAudioGuide.Platforms.iOS.TextToSpeechService))]
namespace VinhThucAudioGuide.Platforms.iOS
{
    public class TextToSpeechService : VinhThucAudioGuide.Services.IPlatformTts
    {
        public void Speak(string text, string locale = null)
        {
            var synth = new AVSpeechSynthesizer();
            var voice = !string.IsNullOrEmpty(locale) ? AVSpeechSynthesisVoice.FromLanguage(locale) : AVSpeechSynthesisVoice.FromLanguage("en-US");
            var utterance = new AVSpeechUtterance(text)
            {
                Rate = AVSpeechUtterance.DefaultSpeechRate,
                Voice = voice
            };
            synth.SpeakUtterance(utterance);
        }
    }
}
