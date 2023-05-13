using OpenAI_API;

namespace StarmaidIntegrationComputer.Chat
{
    public class ChatWindowFactory
    {
        private readonly OpenAIAPI api;
        private readonly string jailbreakMessage;
        public ChatWindowFactory(OpenAIAPI api, JailbreakMessage jailbreakMessage)
        {
            this.api = api;
            this.jailbreakMessage = jailbreakMessage.Value;
        }

        public ChatWindow CreateNew()
        {
            return new ChatWindow(api, jailbreakMessage);

        }
    }
}
