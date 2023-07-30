namespace StarmaidIntegrationComputer.Thalassa.Chat
{
    public class GptPrompt
    {
        public string Value { get; private set; }
        public GptPrompt(string value)
        {
            Value = value;
        }
    }
}
