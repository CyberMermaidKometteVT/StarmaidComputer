using Microsoft.Extensions.Logging;

using OpenAI_API;

namespace StarmaidIntegrationComputer.Chat
{
    public class ChatWindowFactory
    {
        private readonly OpenAIAPI api;
        private readonly string jailbreakMessage;
        private readonly ILogger<ChatComputer> logger;

        public ChatWindowFactory(OpenAIAPI api, ILogger<ChatComputer> logger, JailbreakMessage jailbreakMessage)
        {
            this.api = api;
            this.logger = logger;
            this.jailbreakMessage = jailbreakMessage.Value;
        }

        public ChatWindow CreateNew()
        {
            return new ChatWindow(api, logger, jailbreakMessage);

        }
    }
}
