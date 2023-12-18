using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StarmaidIntegrationComputer.Twitch.Authorization.Models;

using TwitchLib.Api;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using Microsoft.Extensions.Logging;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using StarmaidIntegrationComputer.Twitch.Authorization;
using StarmaidIntegrationComputer.StarmaidSettings;
using StarmaidIntegrationComputer.Thalassa.SpeechSynthesis;
using StarmaidIntegrationComputer.Thalassa.Chat;
using StarmaidIntegrationComputer.Commands;
using StarmaidIntegrationComputer.Helpers;
using StarmaidIntegrationComputer.Twitch;
using TwitchLib.Client.Events;
using StarmaidIntegrationComputer.Common.DataStructures.StarmaidState;
using OpenAI.ObjectModels.RequestModels;
using StarmaidIntegrationComputer.Common;
using StarmaidIntegrationComputer.Thalassa.Settings;

namespace StarmaidIntegrationComputer
{
    //TODO: THIS IS A SUPERCLASS THAT SHOULD PROBABLY GET BROKEN OUT FURTHER!
    public class IntegrationComputerCore
    {
        private bool isRunningUsePropertyOnly;
        public bool IsRunning
        {
            get { return isRunningUsePropertyOnly; }
            private set
            {
                isRunningUsePropertyOnly = value;
                if (UpdateIsRunningVisuals != null) UpdateIsRunningVisuals();
            }
        }

        private readonly TwitchSensitiveSettings twitchSensitiveSettings;
        private readonly TwitchSettings twitchSettings;

        public TwitchAuthorizationUserTokenFlowHelper AuthorizationHelper { get; }


        private TwitchAPI twitchConnection;
        private TwitchPubSub pubSub;
        private TwitchClient chatbot = new TwitchClient();
        public readonly ILogger<IntegrationComputerCore> logger;
        private readonly ILogger<TwitchPubSub> pubSubLogger;
        private readonly ILogger<TwitchClient> chatbotLogger;
        private readonly SpeechComputer speechComputer;
        private readonly CommandFactory commandFactory;

        private readonly StarmaidStateBag commandStateBag;
        private ChatComputer activeChatComputerUsePropertyOnly;
        public ChatComputer ActiveChatComputer
        {
            get { return activeChatComputerUsePropertyOnly; }
            set
            {
                activeChatComputerUsePropertyOnly = value;

                if (!ActiveChatComputer.OutputChatbotChattingMessageHandlers.Contains(speechComputer.SpeakFakeAsync))
                {
                    ActiveChatComputer.OutputChatbotChattingMessageHandlers.Add(speechComputer.SpeakFakeAsync);
                }

                if (!ActiveChatComputer.OutputChatbotCommandHandlers.Contains(ConsiderThalassaResponseAsACommand))
                {
                    ActiveChatComputer.OutputChatbotCommandHandlers.Add(ConsiderThalassaResponseAsACommand);
                }
            }
        }

        private LiveAuthorizationInfo liveTwitchAuthorizationInfo;
        private ThalassaSettings thalassaSettings;

        public Action<string> Output { get; set; }
        public Action UpdateIsRunningVisuals { get; set; }

        public List<CommandBase> ExecutingCommands { get; } = new List<CommandBase> { };

        public IntegrationComputerCore(IntegrationComputerCoreCtorArgs ctorArgs)
        {
            this.twitchSensitiveSettings = ctorArgs.TwitchSensitiveSettings;
            this.twitchSettings = ctorArgs.TwitchSettings;
            this.logger = ctorArgs.LoggerFactory.CreateLogger<IntegrationComputerCore>();
            this.pubSubLogger = ctorArgs.LoggerFactory.CreateLogger<TwitchPubSub>();
            this.AuthorizationHelper = ctorArgs.AuthorizationHelper;
            this.twitchConnection = ctorArgs.TwitchConnection;
            this.chatbotLogger = ctorArgs.LoggerFactory.CreateLogger<TwitchClient>();
            this.speechComputer = ctorArgs.SpeechComputer;
            this.commandStateBag = ctorArgs.StateBag;
            this.liveTwitchAuthorizationInfo = ctorArgs.LiveTwitchAuthorizationInfo;
            this.thalassaSettings = ctorArgs.ThalassaSettings;

            ILogger<CommandBase> commandBaseLogger = ctorArgs.LoggerFactory.CreateLogger<CommandBase>();

            ctorArgs.AuthorizationHelper.OnAuthorizationProcessSuccessful = SetAccessTokenOnGetAccessTokenContinue;
            ctorArgs.AuthorizationHelper.OnAuthorizationProcessFailed = AuthorizationProcessFailed;
            ctorArgs.AuthorizationHelper.OnAuthorizationProcessUserCanceled = AuthorizationProcessUserCanceled;

            IsRunning = twitchSettings.RunOnStartup;
            commandFactory = new CommandFactory(commandBaseLogger, twitchSensitiveSettings,thalassaSettings, speechComputer, chatbot, liveTwitchAuthorizationInfo, twitchConnection, ctorArgs.StateBag);
        }

        private void AuthorizationProcessUserCanceled()
        {
            this.IsRunning = false;
        }

        private void AuthorizationProcessFailed(string _)
        {
            this.IsRunning = false;
        }

        private void SetAccessTokenOnGetAccessTokenContinue(AccessToken accessToken)
        {
            Task.Factory.StartNew(async () => await SetAccessTokenOnGetAccessTokenContinueAsync(accessToken));
        }

        private async Task SetAccessTokenOnGetAccessTokenContinueAsync(AccessToken accessToken)
        {
            liveTwitchAuthorizationInfo.AccessToken = accessToken ?? throw new ArgumentNullException(nameof(accessToken));


            pubSubLogger.LogInformation("Instantiating pub sub");
            pubSub = new TwitchPubSub(pubSubLogger);

            User thalassaUserId = (await twitchConnection.Helix.Users
                .GetUsersAsync(logins: new List<string> { twitchSensitiveSettings.TwitchApiUsername })).Users.Single();
            liveTwitchAuthorizationInfo.ThalassaUserId = thalassaUserId.Id;



            User broadcastingTwitchUser = (await twitchConnection.Helix.Users
                .GetUsersAsync(logins: new List<string> { twitchSensitiveSettings.TwitchChatbotChannelName })).Users.Single();
            liveTwitchAuthorizationInfo.StreamerBroadcasterId = broadcastingTwitchUser.Id;

            StartListeningToTwitchApi();
            ConnectChatbot();
        }

        internal Task ConsiderThalassaResponseAsACommand(FunctionCall thalassaResponse)
        {
            Dictionary<string, object>? arguments = thalassaResponse.ParseArguments();


            var command = commandFactory.Parse(thalassaResponse.Name, arguments);
            ExecutingCommands.Add(command);
            OnCommandListChanged(ExecutingCommands.Count);

            command.OnCompleteActions.Add(ClearCommandFromExecutionList);
            command.OnAbortActions.Add(ClearCommandFromExecutionList);

            command.Execute();

            return Task.CompletedTask;
        }

        private void ClearCommandFromExecutionList(CommandBase command)
        {
            ExecutingCommands.Remove(command);
            OnCommandListChanged(ExecutingCommands.Count);
        }

        private void StartListeningToTwitchApi()
        {
            pubSub.OnPubSubServiceConnected += pubSub_ServiceConnected;
            pubSub.OnListenResponse += pubSub_OnListenResponse;
            pubSub.OnStreamUp += pubSub_StreamUp;
            pubSub.OnStreamDown += pubSub_StreamDown;
            pubSub.OnChannelPointsRewardRedeemed += PubSub_OnChannelPointsRewardRedeemed;
            pubSub.OnRaidUpdateV2 += PubSub_OnRaidUpdateV2;

            pubSub.Connect();
        }

        private void ConnectChatbot()
        {
            //TODO: If I decide I don't need all the permissions, specify which I do here!
            var chatbotCapabilities = new Capabilities(true, true, true);

            var chatbotConnectionCredentials = new ConnectionCredentials(twitchSensitiveSettings.TwitchChatbotUsername, liveTwitchAuthorizationInfo.AccessToken.Token, capabilities: chatbotCapabilities);

            chatbot.WillReplaceEmotes = true; //No idea what the default is here
            chatbot.AutoReListenOnException = true;
            chatbot.Initialize(chatbotConnectionCredentials, twitchSensitiveSettings.TwitchChatbotChannelName, twitchSettings.ChatCommandIdentifier, twitchSettings.WhisperCommandIdentifier);

            chatbot.OnConnected += Chatbot_OnConnected;
            chatbot.OnConnectionError += Chatbot_OnConnectionError;
            chatbot.OnError += Chatbot_OnError;
            chatbot.OnLog += Chatbot_OnLog;
            chatbot.OnNoPermissionError += Chatbot_OnNoPermissionError;
            chatbot.OnSelfRaidError += Chatbot_OnSelfRaidError;
            chatbot.OnMessageReceived += Chatbot_OnMessageReceived;
            chatbot.OnLeftChannel += Chatbot_OnLeftChannel;
            chatbot.OnRaidNotification += Chatbot_OnRaidNotification;
            chatbot.OnUserJoined += Chatbot_OnUserJoined;
            chatbot.OnUserLeft += Chatbot_OnUserLeft;
            chatbot.OnExistingUsersDetected += Chatbot_OnExistingUsersDetected;
            chatbot.OnJoinedChannel += Chatbot_OnJoinedChannel;
            //chatbot.OnUserTimedout += Chatbot_OnUserTimedout; //Do we want a timed-out user list?  This is the Twitch temporary chat ban, not a leave event!

            chatbotLogger.LogInformation("Connecting to chat bot!");
            bool success = chatbot.Connect();
            if (!success)
            {
                logger.LogError("Chatbot failed to attempt to connect!  (No calls were made to Twitch?)");
            }
            else
            {
                chatbot.JoinChannel(twitchSensitiveSettings.TwitchChatbotChannelName);
                logger.LogInformation("Chatbot connecting... successfully!");
            }
        }

        private void Chatbot_OnJoinedChannel(object? sender, OnJoinedChannelArgs e)
        {
            chatbotLogger.LogInformation($"Chatbot succesfully joined channel: {e.Channel}");
        }

        private void Chatbot_OnLeftChannel(object? sender, TwitchLib.Client.Events.OnLeftChannelArgs e)
        {
            logger.LogInformation($"Thalassa has just left the {e.Channel} channel.");
        }


        private void Chatbot_OnExistingUsersDetected(object? sender, OnExistingUsersDetectedArgs e)
        {
            commandStateBag.Viewers.AddRange(e.Users);
        }

        private void Chatbot_OnUserLeft(object? sender, TwitchLib.Client.Events.OnUserLeftArgs e)
        {
            commandStateBag.Viewers.Remove(e.Username);
        }

        private void Chatbot_OnUserJoined(object? sender, TwitchLib.Client.Events.OnUserJoinedArgs e)
        {
            if (!commandStateBag.Viewers.Contains(e.Username))
            {
                commandStateBag.Viewers.Add(e.Username);
            }
        }

        private void Chatbot_OnRaidNotification(object? sender, TwitchLib.Client.Events.OnRaidNotificationArgs e)
        {
            chatbotLogger.LogInformation($"Raid notification - {e.RaidNotification.DisplayName}");

            DateTime raidTimestamp = TmiSentTsHelpers.ParseOrNow(e.RaidNotification.TmiSentTs);

            RaiderInfo? previousRaider = commandStateBag.Raiders.SingleOrDefault(previousRaider => previousRaider.RaiderName == e.RaidNotification.DisplayName);


            if (previousRaider == null)
            {
                RaiderInfo raider = new RaiderInfo
                {
                    RaiderName = e.RaidNotification.DisplayName,
                    RaidTime = raidTimestamp,
                    LastShoutedOut = null
                };

                commandStateBag.Raiders.Add(raider);
            }
            else
            {
                previousRaider.RaidTime = raidTimestamp;
            }
        }
        private void Chatbot_OnLog(object? sender, TwitchLib.Client.Events.OnLogArgs e)
        {
            string sanitizedlogData = StringManipulation.SanitizeForRichTextBox(e.Data);
            chatbotLogger.LogInformation($"Chatbot logs: Chatbot {e.BotUsername} logs {sanitizedlogData}");
        }

        private void Chatbot_OnMessageReceived(object? sender, TwitchLib.Client.Events.OnMessageReceivedArgs e)
        {
            //TODO: Change the log level of this action
            string sanitizedMessageReceived = StringManipulation.SanitizeForRichTextBox(e.ChatMessage.Message);
            chatbotLogger.LogInformation($"Message received - {e.ChatMessage.DisplayName}: {sanitizedMessageReceived}");

            if (!commandStateBag.Chatters.Any(chatter => chatter.ChatterName == e.ChatMessage.DisplayName))
            {
                DateTime sentTimestamp = TmiSentTsHelpers.ParseOrNow(e.ChatMessage.TmiSentTs);

                //Have a breakpoint here to see if the timestamp is a reasonable number - it might be a FromUnixTimeMilliseconds instead of a FromUnixTimeSeconds.

                var messageInfo = new ChatterMessageInfo { Message = sanitizedMessageReceived, Timestamp = sentTimestamp };

                Chatter newChatter = new Chatter(e.ChatMessage.DisplayName, messageInfo);

                commandStateBag.Chatters.Add(newChatter);
            }
        }

        private void Chatbot_OnSelfRaidError(object? sender, EventArgs e)
        {
            chatbotLogger.LogError("Chatbot self-raid error - you cannot raid yourself! ☹");
        }

        private void Chatbot_OnNoPermissionError(object? sender, EventArgs e)
        {
            chatbotLogger.LogError("Chatbot error: Permission denied on the Twitch side.  Twitch offers no further information.");
        }

        private void Chatbot_OnError(object? sender, TwitchLib.Communication.Events.OnErrorEventArgs e)
        {
            chatbotLogger.LogError("Chatbot error:");
            chatbotLogger.LogError(e.Exception, null);
        }

        private void Chatbot_OnConnectionError(object? sender, TwitchLib.Client.Events.OnConnectionErrorArgs e)
        {
            chatbotLogger.LogError($"Chatbot {e.BotUsername} connection error: {e.Error.Message}");
        }

        private void Chatbot_OnConnected(object? sender, TwitchLib.Client.Events.OnConnectedArgs e)
        {
            chatbotLogger.LogInformation($"Chatbot {e.BotUsername} connected successfully to {e.AutoJoinChannel}!");

            chatbot.SendMessage(twitchSensitiveSettings.TwitchChatbotChannelName, "Thalassa connected successfully!");
        }

        private void pubSub_ServiceConnected(object? sender, EventArgs e)
        {
            pubSub.ListenToChannelPoints(liveTwitchAuthorizationInfo.ThalassaUserId);
            pubSub.ListenToRaid(liveTwitchAuthorizationInfo.ThalassaUserId);
            pubSub.SendTopics(liveTwitchAuthorizationInfo.AccessToken.Token);
        }

        #region pubSub listener event handlers
        private int consecutiveFailedListenResponseCount = 0;
        public Action<int> OnCommandListChanged = null;

        private void pubSub_OnListenResponse(object? sender, OnListenResponseArgs e)
        {
            var logMessageText = $"On Listen Response firing for topic {e.Topic}, Success: {e.Response.Successful}, Channel ID: {e.ChannelId}, Error: {e.Response.Error}";

            if (e.Successful)
            {
                logger.LogInformation(logMessageText);
            }
            if (!e.Successful)
            {
                consecutiveFailedListenResponseCount++;
                logger.LogWarning($"{consecutiveFailedListenResponseCount} failures: {logMessageText}");

                if (e.Response.Error == "ERR_BADAUTH")
                {
                    if (consecutiveFailedListenResponseCount > 6)
                    {
                        logger.LogError($"Not attempting to refresh auth token again - too many consecutive failures.");
                        return;
                    }
                    RefreshAuthToken();
                }
            }
        }

        private void RefreshAuthToken()
        {
            var refreshResponseTask = twitchConnection.Auth.RefreshAuthTokenAsync(liveTwitchAuthorizationInfo.AccessToken.RefreshToken, twitchSensitiveSettings.TwitchClientSecret, twitchSensitiveSettings.TwitchClientId);

            refreshResponseTask.ContinueWith(async responseTask =>
            {
                //TODO: Consider further error handling
                var response = await responseTask;
                var accessToken = AuthorizationHelper.GetAccessToken(response);
                this.liveTwitchAuthorizationInfo.AccessToken = accessToken;
                logger.LogInformation("Token refreshed.");
                pubSub.SendTopics(accessToken.Token);
            });
        }

        private void PubSub_OnTimeout(object? sender, OnTimeoutArgs e)
        {
            Output($"Timed out user - {e.TimedoutUser}!");
        }

        private void PubSub_OnRaidUpdateV2(object? sender, OnRaidUpdateV2Args e)
        {
            Output($"Raid update - raiding {e.TargetDisplayName}!");
        }

        private void PubSub_OnChannelPointsRewardRedeemed(object? sender, OnChannelPointsRewardRedeemedArgs e)
        {
            string title = e.RewardRedeemed.Redemption.Reward.Title;
            string user = e.RewardRedeemed.Redemption.User.DisplayName;
            string message = e.RewardRedeemed.Redemption.UserInput;
            Output($"Reward redeemed by { user } - { title }{ (message != null ? $" - {message}" : "")}");
        }

        private void pubSub_StreamUp(object? sender, OnStreamUpArgs e)
        {
            Output("Stream up!");
        }

        private void pubSub_StreamDown(object? sender, OnStreamDownArgs e)
        {
            Output("Stream down!");
        }
        #endregion pubSub listener event handlers

        public Task ToggleRunning()
        {
            IsRunning = !IsRunning;
            return EnactIsRunning();
        }

        //USER ACCESS TOKEN FLOW
        //Trying to get user access token with: https://id.twitch.tv/oauth2/authorize?client_id=<client_id_URL_encoded>&redirect_uri=http%3A%2F%2Flocalhost&response_type=code&scope=channel%3Aread%3Aredemptions
        //That comes up with a redirect code, that I then need to plug into the tokens endpoint (I have that recorded in Postman, it's https://id.twitch.tv/oauth2/token ) to get the access token (and a refresh token, because the access token expires every 4 hours; I'll need to make periodic refresh token requests)
        //The ACCESS TOKEN, I can use like I was trying to use the authToken for below like in pubSub.SendTopics().
        // After getting the auth code I need to call Auth.GetAccessTokenFromCodeAsync() to get the access token.
        public Task EnactIsRunning()
        {
            return Task.Factory.StartNew(() =>
            {
                if (IsRunning)
                {
                    if (liveTwitchAuthorizationInfo.AccessToken == null || liveTwitchAuthorizationInfo.AccessToken.ExpiresAt >= DateTime.Now)
                    {
                        AuthorizationHelper.PromptForUserAuthorization();
                        //An event in the prompt will move us on to StartListeningToTwitch() when the time is right.
                    }
                    else
                    {
                        StartListeningToTwitchApi();
                    }

                }
                else //!isRunning
                {
                    pubSub?.Disconnect();
                    if (chatbot.IsConnected)
                    {
                        chatbot.Disconnect();
                    }
                }

                if (UpdateIsRunningVisuals != null) UpdateIsRunningVisuals();
                return Task.CompletedTask;
            });
        }

    }
}
