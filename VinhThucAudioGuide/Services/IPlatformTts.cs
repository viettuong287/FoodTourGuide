namespace VinhThucAudioGuide.Services
{
    public interface IPlatformTts
    {
        void Speak(string text, string locale = null);
    }
}
