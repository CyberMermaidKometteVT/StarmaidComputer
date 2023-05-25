namespace StarmaidIntegrationComputer.Thalassa.VoiceToText.Exceptions
{
    public class TranscriptionSenderException : Exception
    {
        public string TranscriptionResponseJson { get; private set; }

        public TranscriptionSenderException(string? message, string transcriptionResponseJson) : base(message)
        {
            TranscriptionResponseJson = transcriptionResponseJson;
        }
    }
}
