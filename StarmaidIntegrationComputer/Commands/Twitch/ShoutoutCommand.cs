using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using StarmaidIntegrationComputer.Commands.State;
using StarmaidIntegrationComputer.Commands.Twitch.CommandHelpers;
using StarmaidIntegrationComputer.StarmaidSettings;
using StarmaidIntegrationComputer.Thalassa.SpeechSynthesis;
using StarmaidIntegrationComputer.Twitch;

using TwitchLib.Api;
using TwitchLib.Api.Core.Exceptions;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using TwitchLib.Client;

namespace StarmaidIntegrationComputer.Commands.Twitch
{
    internal class ShoutoutCommand : TwitchCommandBase
    {
        public string ShoutoutTarget { get; private set; }
        public ShoutoutCommand(ILogger<CommandBase> logger, SpeechComputer speechComputer, TwitchSensitiveSettings twitchSensitiveSettings, TwitchClient chatbot, LiveAuthorizationInfo liveAuthorizationInfo, TwitchAPI twitchApi, string target)
            : base(logger, speechComputer, Enums.TwitchStateToValidate.ChatbotAndApi, liveAuthorizationInfo, twitchSensitiveSettings, twitchApi, chatbot)
        {
            this.ShoutoutTarget = target;
        }

        protected override async Task PerformCommandAsync()
        {
            //TODO: Consider replacing the "flies by {ShoutoutTarget}" with a nickname, if one is set later!

            string couldNotFindUserMessage = $"Couldn't find Twitch user {ShoutoutTarget} to shout them out.";
            ShoutoutCommandState state;
            try
            {
                state = await GetStateFromTargetChannel();

                if (!state.IsValidUser)
                {
                    speechComputer.Speak(couldNotFindUserMessage);
                    return;
                }
            }
            catch (BadRequestException)
            {
                speechComputer.Speak($"{couldNotFindUserMessage } - did I think the username had invalid characters?");
                return;
            }

            string[] categoriesThatAreVerbs = { "Just Chatting" };

            if (!categoriesThatAreVerbs.Contains(state.LastCategoryName))
            {
                state.LastCategoryName = $"playing {state.LastCategoryName}";
            }


            chatbot.SendMessage(twitchSensitiveSettings.TwitchChatbotChannelName, $"Everyone check it out as the Starmaid flies by @{ShoutoutTarget}, at https://twitch.tv/{ShoutoutTarget} where they were last {state.LastCategoryName}.  \"{state.LastTitle}\"{state.InterestingTagCommentary}");

            //TODO: Once the enum list for the scopes includes the right scope for this, this is how
            //  we do a /shoutout! :D
            //await twitchApi.Helix.Chat.SendShoutoutAsync(liveAuthorizationInfo.BroadcasterId, state.RecipientBroadcasterId, settings.TwitchClientId, liveAuthorizationInfo.AccessToken.Token);
        }

        private async Task<ShoutoutCommandState> GetStateFromTargetChannel()
        {
            ShoutoutCommandState state = new ShoutoutCommandState();
#warning Error handling here if we are shouting out a non name - it's a BadRequestException.
            GetUsersResponse targets = await twitchApi.Helix.Users.GetUsersAsync(logins: new List<string> { ShoutoutTarget });

            if (targets.Users.Count() == 0)
            {
                state.LastCategoryName = "<User not found, can't look up information>";
                state.IsValidUser = false;
                return state;
            }

            state.RecipientBroadcasterId = targets.Users.First().Id;

            var channelInformationResponse = await twitchApi.Helix.Channels.GetChannelInformationAsync(state.RecipientBroadcasterId);

            if (channelInformationResponse.Data.Length == 0)
            {
                state.LastCategoryName = "Found the user but not their channel, can't look up data about them.";
                state.IsValidUser = false;
                return state;
            }

            state.LastCategoryName = channelInformationResponse.Data.First().GameName;
            state.LastTitle = channelInformationResponse.Data.First().Title;
            state.InterestingTagCommentary = TwitchTagDescriber.GetInterestingTagCommentary(channelInformationResponse.Data.First().Tags);
            state.IsValidUser = true;

            return state;
        }

    }
}
