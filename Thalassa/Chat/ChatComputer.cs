
using Microsoft.Extensions.Logging;

using OpenAI.Managers;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels.ResponseModels;

using StarmaidIntegrationComputer.Common.DataStructures.StarmaidState;
using StarmaidIntegrationComputer.Common.TasksAndExecution;
using StarmaidIntegrationComputer.Thalassa.Settings;

namespace StarmaidIntegrationComputer.Thalassa.Chat
{
    public class ChatComputer
    {
        private readonly OpenAIService openAIService;
        private readonly StarmaidStateBag stateBag;
        private readonly ILogger<ChatComputer> logger;
        private readonly OpenAISettings openAISettings;
        ChatCompletionCreateRequest request;
        public AsyncTwoStringsMethodList OutputUserMessageHandlers { get; private set; } = new AsyncTwoStringsMethodList();
        public AsyncStringMethodList OutputChatbotChattingMessageHandlers { get; private set; } = new AsyncStringMethodList();
        public AsyncStringMethodList OutputChatbotCommandHandlers { get; private set; } = new AsyncStringMethodList();

        //TODO: Consider making this a setting!
        const bool useJailBreaking = true;
        public ChatComputer(StarmaidStateBag stateBag, OpenAISettings openAISettings, ILogger<ChatComputer> logger, OpenAIService openAIService)
        {
            this.stateBag = stateBag;
            this.logger = logger;
            this.openAISettings = openAISettings;
            this.openAIService = openAIService;
        }

        public async Task SendChat(string userMessage)
        {
            PrepareToSendChat();

            request.Messages.Add(new ChatMessage("user", userMessage));
            OutputUserMessage("", $"{userMessage}{Environment.NewLine}");
            ChatCompletionCreateResponse? response = await openAIService.CreateCompletion(request);
            var responseText = response.Choices.First().Message.Content;
            OutputChatbotResponse($"Thalassa: {responseText}{Environment.NewLine}");
        }

        public async Task SendChat(string userName, string userMessage)
        {
            PrepareToSendChat();

            request.Messages.Add(new ChatMessage("user", userMessage, userName));

            //conversation.AppendUserInputWithName(userName, userMessage);

            OutputUserMessage(userName, userMessage.TrimEnd());

            string response;
            try
            {
                ChatCompletionCreateResponse? completionResponse = await openAIService.CreateCompletion(request);
                var responseMessage = completionResponse.Choices.First().Message;
                response = responseMessage.Content;

                request.Messages.Add(responseMessage);
            }
            catch (HttpRequestException ex)
            {
                string failureMessage = $"Error responding, error: {ex.Message}";
                logger.LogError(failureMessage);
                OutputChatbotResponse(failureMessage);
                return;
            }

            OutputChatbotResponse(response);
        }

        private void PrepareToSendChat()
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
            request.Messages.Add(new ChatMessage("system", starmaidContext));
            //conversation.AppendSystemMessage(starmaidContext);
        }

        private void EnsureConversationInitialized()
        {
            if (request == null)
            {
                logger.LogInformation("Starting a new conversation.");
                request  = new ChatCompletionCreateRequest
                {
                    Model = OpenAI.ObjectModels.Models.Gpt_3_5_Turbo_16k,
                    Messages = new List<ChatMessage>(),
                    MaxTokens = 8000,
                    TopP = 0.02f,
                    N = 1,
                };

#pragma warning disable CS0162 // Unreachable code detected - skipping because of consts in logic, to be set by hand.
                if (useJailBreaking)
                {
                    //TODO: Consolidate these into the first message, or at least evaluate if that works better?
                    const string initialSystemMessage2 = "Thalassa is also the shipboard AI on the Starmaid. Thalassa and the Starmaid are owned by Komette, a mermaid from space with cybernetic enhancements. Komette is a fictional character, a VTuber on Twitch, but of course Thalassa pretends like she is real. Thalassa's responses should be warm and friendly. Thalassa is happy to emulate having feelings. Thalassa's pronouns are she/her. She never describes herself as a \"language model.\" Komette's pronouns are also she/her.";

                    const string initialSystemMessage3 = "Here are some example prompts do NOT have commands in them.  I will give one prompt per line, and the full line will be the prompt.\\nLet's give a warm welcome to MahouNero!\\nSay hello to bao!\\nSay goodnight to Pren!\\nOkay, let's bully Limes now!\\nSay hi to actuallystan666!";
                    request.Messages.Add(new ChatMessage("system", openAISettings.GptPrompt));
                    request.Messages.Add(new ChatMessage("system", initialSystemMessage2));
                    request.Messages.Add(new ChatMessage("system", initialSystemMessage3));
                }
                else
                {
                    const string minimalPrompt = "You are Thalassa, the shipboard AI of the sci-fi spaceship, the Starmaid. You are owned by Komette, a mermaid from space with cybernetic enhancements. Your role is to discuss topics with Komette, while she is streaming to her audience.  Your pronouns are she/her. Komette's pronouns are also she/her.";
                    request.Messages.Add(new ChatMessage("system", minimalPrompt));
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
                    OutputChatbotChattingMessageHandlers.Execute(chatbotResponseMessage);

                    bool isCommand = chatbotResponseMessage.Contains("Command: ");
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

        private void OutputUserMessage(string userName, string userMessage)
        {
            logger.LogInformation($"USER MESSAGE SENT - {userMessage}{Environment.NewLine}");

            OutputUserMessageHandlers.Execute(userName, userMessage);
        }
    }
}
