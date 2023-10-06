namespace StarmaidIntegrationComputer.Thalassa.Settings
{
    public class OpenAISettings
    {
        public string GptChatPrompt { get; set; }
        public string GptCommandPrompt { get; set; }
        public string[] CommandPrefixPhrases { get; set; }
    }
}
