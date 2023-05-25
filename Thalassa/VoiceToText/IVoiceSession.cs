
namespace Thalassa.VoiceToText
{
    public interface IVoiceSession
    {
        Task<byte[]> ListeningTask { get; }

        Task<byte[]> Start();
    }
}