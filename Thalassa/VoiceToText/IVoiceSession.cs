namespace StarmaidIntegrationComputer.Thalassa.VoiceToText
{
    public interface IVoiceSession
    {
        Task<byte[]> ListeningTask { get; }

        Task<byte[]> Start();
        void Cancel();
    }
}