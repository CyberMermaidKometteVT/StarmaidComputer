namespace StarmaidIntegrationComputer.Thalassa.SpeechSynthesis
{
    public interface IOpenAiTtsDispatcher
    {
        bool IsSpeaking { get; }
        Action DoneSpeaking { get; set; }

        void Abort();
        void Speak(string textToSpeak);
    }
}