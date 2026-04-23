namespace VinhThucAudioGuide.Services
{
    public interface ITextToSpeech
    {
        void Speak(string text, string locale = null);
    }
}
