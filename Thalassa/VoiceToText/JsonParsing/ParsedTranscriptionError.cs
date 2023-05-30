namespace StarmaidIntegrationComputer.Thalassa.VoiceToText.JsonParsing
{
    public class ParsedTranscriptionError
    {
        public class ParsedError
        {
            public string message { get; set; }
            public string type { get; set; }
            public string param { get; set; }
            public string code { get; set; }
        }

        public ParsedError error { get; set; }
    }
}