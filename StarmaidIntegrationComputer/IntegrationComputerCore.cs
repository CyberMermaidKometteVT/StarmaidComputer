using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StarmaidIntegrationComputer.Twitch.Authorization.Models;

using TwitchLib.Api;
using TwitchLib.Api.Core.Enums;
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
using System.Text.RegularExpressions;
using StarmaidIntegrationComputer.Commands;

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

        private readonly List<AuthScopes> Scopes = new List<AuthScopes> { AuthScopes.Helix_Channel_Read_Redemptions };
        private readonly Dictionary<string, ulong> roleIds = new Dictionary<string, ulong>();
        private Settings settings;
        public TwitchAuthorizationUserTokenFlowHelper AuthorizationHelper { get; }

        private readonly bool ForceTwitchLoginPrompt = false;

        public List<string> Raiders {get; private set;} = new List<string>();
        public List<string> Chatters { get; private set; } = new List<string>();

        public const string LAST_RAIDER_VERBIAGE = "the last raider";

        private TwitchAPI twitchConnection;
        private TwitchPubSub pubSub;
        private TwitchClient chatbot = new TwitchClient();
        private JoinedChannel chatbotJoinedChannel;
        public readonly ILogger<IntegrationComputerCore> logger;
        private readonly ILogger<TwitchPubSub> pubSubLogger;
        private readonly ILogger<TwitchClient> chatbotLogger;
        private readonly SpeechComputer speechComputer;
        private readonly CommandFactory commandFactory;
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

        string? broadcasterId;
        AccessToken accessToken;  // THIS IS THE LONG-TERM ONE!

        public Action<string> Output { get; set; }
        public Action UpdateIsRunningVisuals { get; set; }


        /// <summary>
        /// This is used to prevent CSRF attacks, by having a string that's state-specific
        /// included in the application, so that if we see a response, we can verify
        /// that it was sent in response to our current session.  This must be different
        /// for each session to ensure this.
        /// </summary>
        /// <remarks>TODO: Mix this up between calls, just not per session.  (If Twitch likes that.)</remarks>

        public IntegrationComputerCore(ILogger<IntegrationComputerCore> logger, ILogger<TwitchPubSub> pubSubLogger, ILogger<TwitchClient> clientLogger, ILogger<CommandBase> commandBaseLogger, Settings settings, TwitchAuthorizationUserTokenFlowHelper authorizationHelper, TwitchAPI twitchConnection, SpeechComputer speechComputer)
        {
            this.settings = settings;
            this.logger = logger;
            this.pubSubLogger = pubSubLogger;
            this.AuthorizationHelper = authorizationHelper;
            this.twitchConnection = twitchConnection;
            this.chatbotLogger = clientLogger;
            this.speechComputer = speechComputer;

            authorizationHelper.ForceTwitchLoginPrompt = ForceTwitchLoginPrompt;
            authorizationHelper.OnAuthorizationProcessSuccessful = SetAccessTokenOnGetAccessTokenContinue;
            authorizationHelper.OnAuthorizationProcessFailed = AuthorizationProcessFailed;
            authorizationHelper.OnAuthorizationProcessUserCanceled = AuthorizationProcessUserCanceled;

            IsRunning = settings.RunOnStartup;
            commandFactory = new CommandFactory(commandBaseLogger, settings, speechComputer, chatbot, twitchConnection);
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
            this.accessToken = accessToken ?? throw new ArgumentNullException(nameof(accessToken));


            pubSubLogger.LogInformation("Instantiating pub sub");
            pubSub = new TwitchPubSub(pubSubLogger);

            User broadcastingTwitchUser = (await twitchConnection.Helix.Users
                .GetUsersAsync(logins: new List<string> { settings.TwitchApiUsername })).Users.Single();
            broadcasterId = broadcastingTwitchUser.Id;

            StartListeningToTwitchApi();
            ConnectChatbot();
        }

        internal Task ConsiderThalassaResponseAsACommand(string thalassaResponse)
        {
            Regex commandRegex = new Regex(@"(?:Command: )(?<command>.*)\n(?:Target: )?(?<target>.*)?", RegexOptions.Compiled);
            var matches = commandRegex.Matches(thalassaResponse);

            logger.LogInformation($"Considering if the speech {thalassaResponse} is a command");
            string? commandText = null;
            string? target = null;
            if (matches.Count > 0)
            {
                var match = matches.First();
                if (match.Groups.ContainsKey("command"))
                {
                    commandText = match.Groups["command"].Value;

                    if (match.Groups.ContainsKey("target"))
                    {
                        target = match.Groups["target"].Value;
                    }
                }
            }

            if (commandText != null)
            {
                logger.LogInformation($"The speech {thalassaResponse} was a command: command {commandText}, with target {target}.");

                if (target == LAST_RAIDER_VERBIAGE)
                {
                    if (Raiders.Any())
                    {
                        target = Raiders.Last();
                        logger.LogInformation($"The last raider, by the way, was {target}.");
                    }
                }

#pragma warning disable CS8604 // Possible null reference argument.
                var command = commandFactory.Parse(commandText, target);
#error Immediate to do: 1 - Get the raider and chatter lists to the speech interpreter; 2 - Provide a way to abort commands.
                command.Execute();
#pragma warning restore CS8604 // Possible null reference argument.
            }
            else
            {
                logger.LogInformation($"The speech {thalassaResponse} was not a command.");
            }
            return Task.CompletedTask;
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

            var chatbotConnectionCredentials = new ConnectionCredentials(settings.TwitchChatbotUsername, accessToken.Token, capabilities: chatbotCapabilities);

            chatbot.WillReplaceEmotes = true; //No idea what the default is here
            chatbot.AutoReListenOnException = true;
            chatbot.Initialize(chatbotConnectionCredentials, settings.TwitchChatbotChannelName, settings.ChatCommandIdentifier, settings.WhisperCommandIdentifier);

            chatbot.OnConnected += Chatbot_OnConnected;
            chatbot.OnConnectionError += Chatbot_OnConnectionError;
            chatbot.OnError += Chatbot_OnError;
            chatbot.OnLog += Chatbot_OnLog;
            chatbot.OnNoPermissionError += Chatbot_OnNoPermissionError;
            chatbot.OnSelfRaidError += Chatbot_OnSelfRaidError;
            chatbot.OnMessageReceived += Chatbot_OnMessageReceived;
            chatbot.OnLeftChannel += Chatbot_OnLeftChannel;
            chatbot.OnRaidNotification += Chatbot_OnRaidNotification;

            chatbotLogger.LogInformation("Connecting to chat bot!");
            bool success = chatbot.Connect();
            if (!success)
            {
                logger.LogError("Chatbot failed to attempt to connect!  (No calls were made to Twitch?)");
            }
            else
            {
                chatbot.JoinChannel(settings.TwitchChatbotChannelName);
                chatbotJoinedChannel = chatbot.GetJoinedChannel(settings.TwitchChatbotChannelName);
                logger.LogInformation("Chatbot connecting... successfully!");
            }
        }

        private void Chatbot_OnRaidNotification(object? sender, TwitchLib.Client.Events.OnRaidNotificationArgs e)
        {
            chatbotLogger.LogInformation($"Raid notification - {e.RaidNotification.DisplayName}");
            if (!raiders.Contains(e.RaidNotification.DisplayName))
            {
                raiders.Add(e.RaidNotification.DisplayName);
            }

        }

        private void Chatbot_OnLeftChannel(object? sender, TwitchLib.Client.Events.OnLeftChannelArgs e)
        {
            logger.LogInformation($"Thalassa has just left the {e.Channel} channel.");
        }

        private void Chatbot_OnLog(object? sender, TwitchLib.Client.Events.OnLogArgs e)
        {
            chatbotLogger.LogInformation($"Chatbot logs: Chatbot {e.BotUsername} logs {e.Data}");
        }

        private void Chatbot_OnMessageReceived(object? sender, TwitchLib.Client.Events.OnMessageReceivedArgs e)
        {
            //TODO: Change the log level of this action
            chatbotLogger.LogInformation($"Message received - {e.ChatMessage.DisplayName}: {e.ChatMessage.Message}");

            if (!chatters.Contains(e.ChatMessage.DisplayName))
            {
                chatters.Add(e.ChatMessage.DisplayName);
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

            chatbot.SendMessage(settings.TwitchChatbotChannelName, "Thalassa connected successfully!");
        }

        private void pubSub_ServiceConnected(object? sender, EventArgs e)
        {
            pubSub.ListenToChannelPoints(broadcasterId);
            pubSub.ListenToRaid(broadcasterId);
            pubSub.SendTopics(accessToken.Token);
        }

        #region pubSub listener event handlers
        private int consecutiveFailedListenResponseCount = 0;
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
            var refreshResponseTask = twitchConnection.Auth.RefreshAuthTokenAsync(accessToken.RefreshToken, settings.TwitchClientSecret, settings.TwitchClientId);

            refreshResponseTask.ContinueWith(async responseTask =>
            {
                //TODO: Consider further error handling
                var response = await responseTask;
                var accessToken = AuthorizationHelper.GetAccessToken(response);
                this.accessToken = accessToken;
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
                    if (accessToken == null || accessToken.ExpiresAt >= DateTime.Now)
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
