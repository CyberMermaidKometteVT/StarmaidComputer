using Microsoft.Extensions.Logging;

using OpenAI_API;

using StarmaidIntegrationComputer.Common.DataStructures;
using StarmaidIntegrationComputer.Thalassa.Chat;

namespace StarmaidIntegrationComputer.Chat
{
    public class ChatWindowFactory
    {
        private readonly OpenAIAPI api;
        private readonly StarmaidStateBag stateBag;
        private readonly string jailbreakMessage;
        private readonly ILogger<ChatComputer> logger;

        public ChatWindowFactory(OpenAIAPI api, StarmaidStateBag stateBag, ILogger<ChatComputer> logger, JailbreakMessage jailbreakMessage)
        {
            this.api = api;
            this.stateBag = stateBag;
            this.logger = logger;
            this.jailbreakMessage = jailbreakMessage.Value;
        }

        public ChatWindow CreateNew()
        {
            return new ChatWindow(api, stateBag, logger, jailbreakMessage);

        }
    }
}
