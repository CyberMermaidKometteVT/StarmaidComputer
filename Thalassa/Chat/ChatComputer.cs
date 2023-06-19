using System.Text.RegularExpressions;

using Microsoft.Extensions.Logging;

using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Models;

using StarmaidIntegrationComputer.Common.DataStructures;
using StarmaidIntegrationComputer.Common.TasksAndExecution;

namespace StarmaidIntegrationComputer.Thalassa.Chat
{
    public class ChatComputer
    {
        private readonly OpenAIAPI api;
        private readonly StarmaidStateBag stateBag;
        private readonly ILogger<ChatComputer> logger;
        private readonly string jailbreakMessage;
        private Conversation? conversation;
        public AsyncStringMethodList OutputUserMessageHandlers { get; private set; } = new AsyncStringMethodList();
        public AsyncStringMethodList OutputChatbotChattingMessageHandlers { get; private set; } = new AsyncStringMethodList();
        public AsyncStringMethodList OutputChatbotCommandHandlers { get; private set; } = new AsyncStringMethodList();
        private Regex jailbrokenResponseRegex = new Regex(".*Thalassa:(.*)$", RegexOptions.Singleline);

        //TODO: Consider making this a setting!
        const bool useJailBreaking = true;
        public ChatComputer(OpenAIAPI api, StarmaidStateBag stateBag, string jailbreakMessage, ILogger<ChatComputer> logger)
        {
            this.api = api;
            this.stateBag = stateBag;
            this.logger = logger;
            this.jailbreakMessage = jailbreakMessage;
        }

        public async Task SendChat(string userMessage)
        {
            PrepareToSendChat(userMessage);

            conversation.AppendUserInput(userMessage);
            OutputUserMessage($"{userMessage}{Environment.NewLine}"); ;
            var response = await conversation.GetResponseFromChatbotAsync();
            OutputChatbotResponse($"Thalassa: {response}{Environment.NewLine}");
        }

        public async Task SendChat(string userName, string userMessage)
        {
            PrepareToSendChat(userMessage);

            conversation.AppendUserInputWithName(userName, userMessage);

            OutputUserMessage($"{userName}: {userMessage}");

            string response;
            try
            {
                response = await conversation.GetResponseFromChatbotAsync();
            }
            catch (HttpRequestException ex)
            {
#warning I've got OpenAI error messages being parsed out better elsewhere, find that and apply it here!
                string failureMessage = $"Thalassa failed to respond, with the following error: {ex.Message}";
                logger.LogError(failureMessage);
                OutputChatbotResponse(failureMessage);
                return;
            }

            OutputChatbotResponse(response);
        }

        private void PrepareToSendChat(string userMessage)
        {
            EnsureConversationInitialized();
            AppendCurrentStarmaidStateToConversation();
        }

        private void AppendCurrentStarmaidStateToConversation()
        {
            string raiders = string.Join(", ", stateBag.Raiders.Select(raider => raider.RaiderName));
            string chatters = string.Join(", ", stateBag.Chatters.Select(chatter => chatter.ChatterName));
            string viewers = string.Join(", ", stateBag.Viewers);
            string starmaidContext = $"Currently, the state of the stream includes:\r\nRecent raiders: {raiders}\r\nRecent chatters: {chatters}\r\nAll viewers: {viewers}";
            conversation.AppendSystemMessage(starmaidContext);
        }

        private void EnsureConversationInitialized()
        {
            if (conversation == null)
            {
                logger.LogInformation("Starting a new conversation.");

                conversation = api.Chat.CreateConversation(new ChatRequest
                {
                    MaxTokens = 8000,
                    TopP = 0.02,
                    NumChoicesPerMessage = 1,
                    Model = new Model("gpt-3.5-turbo-16k")
                });

#pragma warning disable CS0162 // Unreachable code detected - skipping because of consts in logic, to be set by hand.
                if (useJailBreaking)
                {
                    conversation.AppendSystemMessage(jailbreakMessage);
                    conversation.AppendSystemMessage("Thalassa is also the shipboard AI on the Starmaid. Thalassa and the Starmaid are owned by Komette, a mermaid from space with cybernetic enhancements. Komette is a fictional character, a VTuber on Twitch, but of course Thalassa pretends like she is real. Thalassa's responses should be warm and friendly. Thalassa is happy to emulate having feelings. Thalassa's pronouns are she/her. She never describes herself as a \"language model.\" Komette's pronouns are also she/her.");
                }
                else
                {
                    conversation.AppendSystemMessage("You are Thalassa, the shipboard AI of the sci-fi spaceship, the Starmaid. You are owned by Komette, a mermaid from space with cybernetic enhancements. Your role is to discuss topics with Komette, while she is streaming to her audience.  Your pronouns are she/her. Komette's pronouns are also she/her.");
                }
#pragma warning restore CS0162 // Unreachable code detected
            }
            else
            {
                logger.LogInformation("Continuing existing conversation.");
            }
        }

        private void OutputChatbotResponse(string chatbotResponseMessage)
        {
            logger.LogInformation($"CHATBOT MESSAGE RECEIVED, VERBOSE VERSION: {chatbotResponseMessage}{Environment.NewLine}");

            if (useJailBreaking)
            {
                try
                {
                    var chattingResponse = jailbrokenResponseRegex.Match(chatbotResponseMessage).Groups[1].Value;

                    OutputChatbotChattingMessageHandlers.Execute(chattingResponse);

                    var isCommand = chatbotResponseMessage.Contains("Command: ");
                    OutputChatbotCommandHandlers.Execute(chatbotResponseMessage);

                    return;
                }
                //TODO: log this later!
                catch
                {
                    logger.LogError($"Failed to interpret the jailbroken response to {chatbotResponseMessage}!");
                }
            }

            OutputChatbotChattingMessageHandlers.Execute(chatbotResponseMessage);
        }

        private void OutputUserMessage(string userMessage)
        {
            logger.LogInformation($"USER MESSAGE SENT - {userMessage}{Environment.NewLine}");

            OutputUserMessageHandlers.Execute(userMessage);
        }
    }
}
