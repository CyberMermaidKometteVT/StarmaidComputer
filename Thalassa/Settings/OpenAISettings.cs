namespace StarmaidIntegrationComputer.Thalassa.Settings
{
    public class OpenAISettings
    {
        public string Model { get; set; } = "gpt-4.1-mini";
        public string GptChatPrompt { get; set; }
        public string GptCommandPrompt { get; set; }
        public string[] CommandPrefixPhrases { get; set; }
    }
}
