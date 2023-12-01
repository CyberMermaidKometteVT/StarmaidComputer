using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using StarmaidIntegrationComputer.Commands.State;
using StarmaidIntegrationComputer.StarmaidSettings;
using StarmaidIntegrationComputer.Thalassa.SpeechSynthesis;
using StarmaidIntegrationComputer.Twitch;

using TwitchLib.Api;
using TwitchLib.Client;

namespace StarmaidIntegrationComputer.Commands.Twitch
{
    internal class ShoutoutCommand : TwitchCommandBase
    {
        public string ShoutoutTarget { get; private set; }
        private TwitchSensitiveSettings twitchSensitiveSettings;

        public ShoutoutCommand(ILogger<CommandBase> logger, SpeechComputer speechComputer, TwitchSensitiveSettings twitchSensitiveSettings, TwitchClient chatbot, LiveAuthorizationInfo liveAuthorizationInfo, TwitchAPI twitchApi, string target) : base(logger, speechComputer, Enums.TwitchStateToValidate.ChatbotAndApi, liveAuthorizationInfo, twitchApi, chatbot)
        {
            this.ShoutoutTarget = target;
            this.twitchSensitiveSettings = twitchSensitiveSettings;
        }

        protected override async Task PerformCommand()
        {
            //TODO: Consider replacing the "flies by {ShoutoutTarget}" with a nickname, if one is set later!

            ShoutoutCommandState state = await GetLastCategory();

            string[] categoriesThatAreVerbs = { "Just Chatting" };

            if (!categoriesThatAreVerbs.Contains(state.LastCategoryName))
            {
                state.LastCategoryName = $"playing {state.LastCategoryName}";
            }

            //TODO: ??? why does the chatbot's Joined Channels list empty??  It's clearly still in there!
            if (chatbot.JoinedChannels.Count() == 0)
            {
                chatbot.JoinChannel(twitchSensitiveSettings.TwitchChatbotChannelName);
            }

            chatbot.SendMessage(twitchSensitiveSettings.TwitchChatbotChannelName, $"Everyone check it out as the Starmaid flies by @{ShoutoutTarget}, at https://twitch.tv/{ShoutoutTarget} where they were last {state.LastCategoryName}");

            //TODO: Once the enum list for the scopes includes the right scope for this, this is how
            //  we do a /shoutout! :D
            //await twitchApi.Helix.Chat.SendShoutoutAsync(liveAuthorizationInfo.BroadcasterId, state.RecipientBroadcasterId, settings.TwitchClientId, liveAuthorizationInfo.AccessToken.Token);
        }

        private async Task<ShoutoutCommandState> GetLastCategory()
        {
            ShoutoutCommandState state = new ShoutoutCommandState();
#warning Error handling here if we are shouting out a non name - it's a BadRequestException.
            var targets = await twitchApi.Helix.Users.GetUsersAsync(logins: new List<string> { ShoutoutTarget });

            if (targets.Users.Count() == 0)
            {
                state.LastCategoryName = "<User not found, can't look up category>";
                return state;
            }

            state.RecipientBroadcasterId = targets.Users.First().Id;

            var channelInformationResponse = await twitchApi.Helix.Channels.GetChannelInformationAsync(state.RecipientBroadcasterId);

            if (channelInformationResponse.Data.Length == 0)
            {
                state.LastCategoryName = "Found the user but not their channel, can't look up category";
                return state;
            }

            state.LastCategoryName = channelInformationResponse.Data.First().GameName;
            return state;
        }
    }
}
