namespace StarmaidIntegrationComputer.StarmaidSettings
{
    public class TwitchSettings
    {
        public bool RunOnStartup { get; set; }
        public char ChatCommandIdentifier { get; set; }
        public char WhisperCommandIdentifier { get; set; }
        public bool ForceTwitchLoginPrompt { get; set; }
        public bool LogInWithIncognitoBrowser { get; set; }
    }
}
