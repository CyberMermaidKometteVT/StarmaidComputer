using System.Text;

using Microsoft.Extensions.Logging;


using StarmaidIntegrationComputer.Common.DataStructures.StarmaidState;
using StarmaidIntegrationComputer.Common.Settings;
using StarmaidIntegrationComputer.Common.TasksAndExecution;
using StarmaidIntegrationComputer.Thalassa.Settings;
using OpenAI.Chat;
using System.ClientModel;
using System.Text.Json;
using Newtonsoft.Json.Linq;

namespace StarmaidIntegrationComputer.Thalassa.Chat
{
    public class ChatComputer
    {
        private readonly StreamerProfileSettings streamerProfileSettings;
        private readonly AudienceRegistry audienceRegistry;
        private readonly ILogger<ChatComputer> logger;
        private readonly OpenAISensitiveSettings openAISensitiveSettings;
        private readonly OpenAISettings openAISettings;
        private readonly List<ChatMessage> chatMessages = new List<ChatMessage>();
        ChatClient request;
        public AsyncTwoStringsMethodList OutputUserMessageHandlers { get; } = new AsyncTwoStringsMethodList();
        public AsyncStringMethodList OutputChatbotChattingMessageHandlers { get; } = new AsyncStringMethodList();
        public AsyncMethodList<IList<ThalassaCommandCallModel>> OutputChatbotCommandHandlers { get; } = new AsyncMethodList<IList<ThalassaCommandCallModel>>();

        private readonly List<ChatTool> toolsAccessibleByStreamerOrder;
        private readonly List<ChatTool> toolsAccessibleDuringStreamerConversation;

        public ChatComputer(AudienceRegistry audienceRegistry, OpenAISettings openAISettings, ILogger<ChatComputer> logger, OpenAISensitiveSettings openAISensitiveSettings, ThalassaToolBuilder thalassaFunctionBuilder, StreamerProfileSettings streamerProfileSettings)
        {
            this.audienceRegistry = audienceRegistry;
            this.logger = logger;
            this.openAISettings = openAISettings;
            this.openAISensitiveSettings = openAISensitiveSettings;
            this.streamerProfileSettings = streamerProfileSettings;
            toolsAccessibleByStreamerOrder = thalassaFunctionBuilder.BuildToolsAccessibleByStreamerOrder();
            toolsAccessibleDuringStreamerConversation = thalassaFunctionBuilder.BuildToolsAccessibleDuringStreamerConversation();

        }

        public async Task SendChat(string userName, string userMessage)
        {
            bool isCommand = GetIsCommand(userName, userMessage);
            PrepareToSendChat(isCommand);

            chatMessages.Add(new UserChatMessage($"{userName}: {userMessage}"));

            OutputUserMessage(userName, userMessage.TrimEnd());

            string responseMessage;
            try
            {
                ChatCompletionOptions options = new ChatCompletionOptions
                {
                    TopP = 0.02f
                };

                if (isCommand)
                {
                    toolsAccessibleByStreamerOrder.ForEach(function => options.Tools.Add(function));
                }
                else
                {
                    toolsAccessibleDuringStreamerConversation.ForEach(function => options.Tools.Add(function));
                }

                ClientResult<ChatCompletion> completedClientResult = await request.CompleteChatAsync(messages: chatMessages, options: options);

                ChatCompletion completion = completedClientResult.Value;


                //Test this: is the refusal in fact null or whitespace if there's no error?
                // Test this: Is the refusal an error like I think it is?
                if (!String.IsNullOrWhiteSpace(completion.Refusal))
                {
                    HandleRefusal(completion.Refusal);
                    return;
                }

                logger.LogInformation($"Chat completion received; finish reason {completedClientResult.Value.FinishReason}");


                if (completion.Content.Count != 0)
                {
                    chatMessages.Add(new AssistantChatMessage(completion.Content));
                    responseMessage = GetResponseMessage(completion.Content);
                    OutputChatbotResponse(responseMessage);
                }

                OutputAndExecuteChatbotToolCalls(completion.ToolCalls);
            }
            catch (HttpRequestException ex)
            {
                string failureMessage = $"Error responding, error: {ex.Message}";
                logger.LogError(failureMessage);
                OutputChatbotResponse(failureMessage);
                return;
            }
        }

        private string GetResponseMessage(ChatMessageContent content)
        {
            StringBuilder messageParts = new StringBuilder();
            foreach (ChatMessageContentPart part in content)
            {
                if (part.Kind == ChatMessageContentPartKind.Text)
                {
                    messageParts.Append(part.Text);
                }
            }
            return messageParts.ToString();
        }

        private void HandleRefusal(string refusal)
        {
            chatMessages.Add(new AssistantChatMessage($"(The chatbot model encountered an error trying to respond to this chat: {refusal})"));
        }

        private bool GetIsCommand(string userName, string userMessage)
        {
            if (userName != streamerProfileSettings.StreamerName)
            {
                return false;
            }

            string[] fillerText = new string[] { "(", ")", "-", "spoken", " " };

            string userMessageWithoutFiller = userMessage;
            foreach (string filler in fillerText)
            {
                userMessageWithoutFiller = userMessageWithoutFiller.Replace(filler, "");
            }


            userMessageWithoutFiller = userMessageWithoutFiller.ToLower();
            bool isCommand = openAISettings.CommandPrefixPhrases.Any(phrase => userMessageWithoutFiller.StartsWith(phrase.ToLower()));

            return isCommand;
        }

        private void OutputAndExecuteChatbotToolCalls(IReadOnlyList<ChatToolCall> toolCalls)
        {
            if (toolCalls.Count == 0)
            {
                return;
            }

            StringBuilder toolCallsDescription = new StringBuilder("Executing: ");
            List<ThalassaCommandCallModel> commandsToExecute = ModelThalassaCommandCalls(toolCalls);

            bool wasFirstCommand = true;
            foreach (ThalassaCommandCallModel commandCallModel in commandsToExecute)
            {
                if (!wasFirstCommand)
                {
                    toolCallsDescription.Append("; ");
                }
                toolCallsDescription.Append(commandCallModel.Name);
                DescribeFunctionArguments(toolCallsDescription, commandCallModel);
                wasFirstCommand = false;
            }

            OutputChatbotResponse(toolCallsDescription.ToString());
            OutputChatbotCommandHandlers.Execute(commandsToExecute);
        }

        private void DescribeFunctionArguments(StringBuilder descriptionBuilder, ThalassaCommandCallModel commandCallModel)
        {
            descriptionBuilder.Append("(");
            bool isFirst = true;
            foreach (ThalassaCommandCallArgument argument in commandCallModel.Arguments)
            {
                if (!isFirst)
                {
                    descriptionBuilder.Append(", ");
                }

                descriptionBuilder.Append(argument.Name)
                    .Append(": ")
                    .Append(argument.SerializedValue);
            }
            descriptionBuilder.Append(")");
        }

        private List<ThalassaCommandCallModel> ModelThalassaCommandCalls(IReadOnlyList<ChatToolCall> toolCalls)
        {
            List<ThalassaCommandCallModel> results = new List<ThalassaCommandCallModel>();
            foreach (ChatToolCall toolCall in toolCalls)
            {
                ThalassaCommandCallModel newModel = new ThalassaCommandCallModel(toolCall.FunctionName);

                using JsonDocument argumentsJsonDocument = JsonDocument.Parse(toolCall.FunctionArguments);
                JObject argumentsJsonObject = JObject.Parse(argumentsJsonDocument.RootElement.ToString());

                foreach (JProperty property in argumentsJsonObject.Properties())
                {

                    //TODO: Does property.Value.ToString() return me the primitive value of the argument?
                    newModel.Arguments.Add(new ThalassaCommandCallArgument(property.Name, property.Value.ToString()));
                }

                results.Add(newModel);
            }

            return results;
        }

        private void PrepareToSendChat(bool isCommand)
        {
            InitializeConversationForMessage(isCommand);
            AppendCurrentStarmaidStateToConversation();
        }

        private void AppendCurrentStarmaidStateToConversation()
        {
            string raiders = string.Join(", ", audienceRegistry.Raiders.Select(raider => raider.RaiderName));
            string chatters = string.Join(", ", audienceRegistry.Chatters.Select(chatter => chatter.ChatterName));
            string viewers = string.Join(", ", audienceRegistry.Viewers);
            string starmaidContext = $"Currently, the state of the stream includes:\r\nRecent raiders: {raiders}\r\nRecent chatters: {chatters}\r\nAll viewers: {viewers}";
            chatMessages.Add(new SystemChatMessage(starmaidContext));
        }

        private void InitializeConversationForMessage(bool isCommand)
        {

            if (request == null)
            {
                logger.LogInformation("Starting a new conversation.");



                request = new ChatClient(model: "gpt-4o-mini",
                    credential: new ApiKeyCredential(openAISensitiveSettings.OpenAIBearerToken));

                if (isCommand)
                {
                    chatMessages.Add(new SystemChatMessage(openAISettings.GptCommandPrompt));
                }
                else
                {
                    //TODO: Add multiple initial prompt functionality to the json
                    chatMessages.Add(new SystemChatMessage(openAISettings.GptChatPrompt));
                }
            }
            else
            {
                string initialPromptContent;
                if (isCommand)
                {
                    initialPromptContent = openAISettings.GptCommandPrompt;
                    chatMessages.Add(new UserChatMessage("The next message will be a command."));
                }
                else // not a command
                {
                    initialPromptContent = openAISettings.GptChatPrompt;
                }

                chatMessages.RemoveAt(0);
                chatMessages.Insert(0, new SystemChatMessage(initialPromptContent));


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

            OutputChatbotChattingMessageHandlers.Execute(chatbotResponseMessage);
        }

        private void OutputUserMessage(string userName, string userMessage)
        {
            logger.LogInformation($"USER MESSAGE SENT - {userMessage}{Environment.NewLine}");

            OutputUserMessageHandlers.Execute(userName, userMessage);
        }
    }
}
