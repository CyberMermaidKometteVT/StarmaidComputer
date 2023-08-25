using System.Text;

using Microsoft.Extensions.Logging;

using OpenAI.Builders;
using OpenAI.Managers;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels.ResponseModels;
using OpenAI.ObjectModels.SharedModels;

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
        public AsyncMethodList<FunctionCall> OutputChatbotCommandHandlers { get; private set; } = new AsyncMethodList<FunctionCall>();

        List<FunctionDefinition> streamerAccessibleThalassaFunctions = ThalassaFunctionBuilder.BuildStreamerAccessibleFunctions();

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
            var firstResponseChoice = response.Choices.First();
            var responseText = firstResponseChoice.Message.Content;
            OutputChatbotResponse($"Thalassa: {responseText}{Environment.NewLine}");
            OutputChatbotFunctionCall(firstResponseChoice);
        }


        public async Task SendChat(string userName, string userMessage)
        {
            PrepareToSendChat();

            request.Messages.Add(new ChatMessage("user", userMessage, userName));

            OutputUserMessage(userName, userMessage.TrimEnd());

            string response;
            try
            {
                ChatCompletionCreateResponse? completionResponse = await openAIService.CreateCompletion(request);
                var firstResponseChoice = completionResponse.Choices.First();
                var responseMessage = firstResponseChoice.Message;
                response = responseMessage.Content;

                if (responseMessage.Content != null)
                {
                    request.Messages.Add(responseMessage);
                }
                else
                {
                    //TODO: Figure out if we need to identify the case where Content is null but there is no function call involved
                    var functionCallMessage = new ChatMessage(responseMessage.Role, "", responseMessage.Name, responseMessage.FunctionCall);
                    request.Messages.Add(functionCallMessage);
                }

                OutputChatbotFunctionCall(firstResponseChoice);

                if (response != null)
                {
                    OutputChatbotResponse(response);
                }
            }
            catch (HttpRequestException ex)
            {
                string failureMessage = $"Error responding, error: {ex.Message}";
                logger.LogError(failureMessage);
                OutputChatbotResponse(failureMessage);
                return;
            }
        }

        private void OutputChatbotFunctionCall(ChatChoiceResponse firstResponseChoice)
        {
            var functionCall = firstResponseChoice.Message.FunctionCall;
            if (functionCall != null)
            {
#warning Replace with better solution!
                StringBuilder executionOutput = new StringBuilder("Executing: ")
                    .Append(functionCall.Name).Append("(");
                if (!String.IsNullOrWhiteSpace(functionCall.Arguments))
                {
                    bool firstArgument = true;
                    foreach (KeyValuePair<string, object> argumentByName in functionCall.ParseArguments())
                    {
                        if (!firstArgument)
                        {
                            executionOutput.Append(",");
                        }

                        firstArgument = false;

                        executionOutput.Append(" ");
                        executionOutput.Append(argumentByName.Key)
                            .Append(": ")
                            .Append(argumentByName.Value);
                    }
                }
                executionOutput.Append(" )");

                OutputChatbotResponse(executionOutput.ToString());
                OutputChatbotCommandHandlers.Execute(functionCall);

            }
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
        }

        private void EnsureConversationInitialized()
        {
            if (request == null)
            {
                logger.LogInformation("Starting a new conversation.");
                request = new ChatCompletionCreateRequest
                {
                    Model = OpenAI.ObjectModels.Models.Gpt_3_5_Turbo_16k,
                    Messages = new List<ChatMessage>(),
                    MaxTokens = 8000,
                    TopP = 0.02f,
                    N = 1,
                };

#pragma warning disable CS0162 // Unreachable code detected - skipping because of consts in logic, to be set by hand.
                    //TODO: Consolidate these into the first message, or at least evaluate if that works better?
                    request.Messages.Add(new ChatMessage("system", openAISettings.GptPrompt));
                    //request.Messages.Add(new ChatMessage("system", initialSystemMessage2));
                    //request.Messages.Add(new ChatMessage("system", initialSystemMessage3));

                request.Functions = streamerAccessibleThalassaFunctions;
#pragma warning restore CS0162 // Unreachable code detected
            }
            else
            {
                logger.LogInformation("Continuing existing conversation.");
            }
        }

        private void OutputChatbotResponse(string chatbotResponseMessage)
        {
            if (String.IsNullOrWhiteSpace(chatbotResponseMessage))
            {
                logger.LogInformation($"MESSAGE-LESS CHATBOT RESPONSE RECEIVED!");
                return;
            }

            logger.LogInformation($"CHATBOT MESSAGE RECEIVED, VERBOSE VERSION: {chatbotResponseMessage}{Environment.NewLine}");

            if (useJailBreaking)
            {
                try
                {
                    OutputChatbotChattingMessageHandlers.Execute(chatbotResponseMessage);

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
