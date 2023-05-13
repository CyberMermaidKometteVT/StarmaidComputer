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
#warning CRITICAL TODO: THE APP IN RELEASE MODE MIGHT NOT BE CLOSING WHEN THE WINDOW IS CLOSED!
#warning Urgent TODO: Test Use refresh token if we're timing out!
#warning Get some issue tracking for viewers to follow along better!
#warning There is currently a hardcoded file path used by the logger.  Put that in a config file, along with loglevels and things.
#warning Remember, Thalassa currently can't voice back at me!  Find decent integration for that.
#warning Don't forget Discord integration soon!
#warning When Thalassa speaks, figure out how to make a png glow with her words.  (Check out https://eruben.itch.io/vts-pog ?  Or maybe VNyan, if I can find a way to have some other app talk to it to tell it to modify a blendshape or something that is tied to brightness of a light? )  
#warning OBS plugin?
#warning Replace all the logic in KruizControl with StarmidIntegrationComputer logic!
#warning Also I'm seeing a shocking number of OperationCanceledExceptions thrown in "OnError in PubSub Websocket connection" when changing network adapters so maybe I should do something about that?
#warning Also I get more errors if I mash the start/stop button really fast
#warning also stop doesn't really work
#warning I should probably make it so I can copy-paste the contents of the text box
#warning add setting to auto close the auth window, in case someone wants to see the explanation about it and close it manually
#warning Also if the browser doesn't launch the behavior is confusing, as we just stop in the middle of authenticating and go no further.  Maybe we should implement a timer to cancel the listening?  And maybe put that in the response page's Javascript.

namespace StarmaidIntegrationComputer
{
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

        private TwitchAPI twitchConnection;
        private TwitchPubSub pubSub;
        private TwitchClient chatbot = new TwitchClient();
        private JoinedChannel chatbotJoinedChannel;
        public readonly ILogger<IntegrationComputerCore> logger;
        private readonly ILogger<TwitchPubSub> pubSubLogger;
        private readonly ILogger<TwitchClient> chatbotLogger;



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

        public IntegrationComputerCore(ILogger<IntegrationComputerCore> logger, ILogger<TwitchPubSub> pubSubLogger, ILogger<TwitchClient> clientLogger, Settings settings, TwitchAuthorizationUserTokenFlowHelper authorizationHelper, TwitchAPI twitchConnection)
        {
            this.settings = settings;
            this.logger = logger;
            this.pubSubLogger = pubSubLogger;
            this.AuthorizationHelper = authorizationHelper;
            this.twitchConnection = twitchConnection;
            this.chatbotLogger = clientLogger;

            authorizationHelper.ForceTwitchLoginPrompt = ForceTwitchLoginPrompt;
            authorizationHelper.OnAuthorizationProcessSuccessful = SetAccessTokenOnGetAccessTokenContinue;
            authorizationHelper.OnAuthorizationProcessFailed = AuthorizationProcessFailed;
            authorizationHelper.OnAuthorizationProcessUserCanceled = AuthorizationProcessUserCanceled;

            IsRunning = settings.RunOnStartup;

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

        private void Chatbot_OnLog(object? sender, TwitchLib.Client.Events.OnLogArgs e)
        {
            chatbotLogger.LogInformation($"Chatbot logs: Chatbot {e.BotUsername} logs {e.Data}");
        }

        private void Chatbot_OnMessageReceived(object? sender, TwitchLib.Client.Events.OnMessageReceivedArgs e)
        {
            //TODO: Change the log level of this action
            chatbotLogger.LogInformation($"Message received - {e.ChatMessage.DisplayName}: {e.ChatMessage.Message}");
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
