using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Models;

using StarmaidIntegrationComputer.Common;

namespace StarmaidIntegrationComputer.Chat
{
    internal class ChatComputer
    {
        private OpenAIAPI api;
        private readonly string jailbreakMessage;
        private Conversation? conversation;
        public AsyncStringMethodList OutputUserMessageHandlers { get; private set; } = new AsyncStringMethodList();
        public AsyncStringMethodList OutputChatbotResponseHandlers { get; private set; } = new AsyncStringMethodList();
        private Regex jailbrokenResponseRegex = new Regex(".*Thalassa:(.*)");

        //TODO: Consider making this a setting!
        const bool useJailBreaking = true;
        public ChatComputer(OpenAIAPI api, string jailbreakMessage)
        {
            this.api = api;
            this.jailbreakMessage = jailbreakMessage;
        }

        public async Task SendChat(string userMessage)
        {
            EnsureConversationInitialized();

            conversation.AppendUserInput(userMessage);
            OutputUserMessage($"{userMessage}{Environment.NewLine}");
            var response = await conversation.GetResponseFromChatbotAsync();
            OutputChatbotResponse($"Thalassa: {response}{Environment.NewLine}");
        }

        public async Task SendChat(string userName, string userMessage)
        {
            EnsureConversationInitialized();

            conversation.AppendUserInputWithName(userName, userMessage);

            OutputUserMessage($"{userName}: {userMessage}{Environment.NewLine}");
            var response = await conversation.GetResponseFromChatbotAsync();
            OutputChatbotResponse($"{response}{Environment.NewLine}");
        }

        private void EnsureConversationInitialized()
        {
            if (conversation == null)
            {
                conversation = api.Chat.CreateConversation(new ChatRequest { MaxTokens = 3000, TopP = 0.02, NumChoicesPerMessage = 1, Model = Model.ChatGPTTurbo });

#pragma warning disable CS0162 // Unreachable code detected - skipping because of consts in logic, to be set by hand.
                if (useJailBreaking)
                {
                    conversation.AppendSystemMessage(jailbreakMessage);
                    conversation.AppendSystemMessage("Thalassa is also the shipboard AI on the Starmaid. Thalassa and the Starmaid are owned by Komette, a mermaid from space with cybernetic enhancements. Komette is a fictional character, a VTuber on Twitch, but of course Thalassa pretends like she is real. Thalassa's responses should always be warm and supportive. Thalassa should also always be calm and collected. Thalassa's pronouns are she/her. Komette's pronouns are also she/her.");
                }
                else
                {
                    conversation.AppendSystemMessage("You are Thalassa, the shipboard AI of the sci-fi spaceship, the Starmaid. You are owned by Komette, a mermaid from space with cybernetic enhancements. Your role is to discuss topics with Komette, while she is streaming to her audience.  Your pronouns are she/her. Komette's pronouns are also she/her.");
                }
#pragma warning restore CS0162 // Unreachable code detected
            }
        }

        private void OutputChatbotResponse(string chatbotResponseMessage)
        {
            if (useJailBreaking)
            {
                try
                {
                    var response = jailbrokenResponseRegex.Match(chatbotResponseMessage).Value;
                    chatbotResponseMessage = response;
                }
                //TODO: log this later!
                catch
                {
                }
            }

            OutputChatbotResponseHandlers.Execute(chatbotResponseMessage);
        }

        private void OutputUserMessage(string userMessage)
        {
            OutputUserMessageHandlers.Execute(userMessage);
        }
    }
}
