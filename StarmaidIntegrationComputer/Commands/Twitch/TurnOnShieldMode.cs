//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;

//using Microsoft.Extensions.Logging;

//using StarmaidIntegrationComputer.Commands.State;
//using StarmaidIntegrationComputer.StarmaidSettings;
//using StarmaidIntegrationComputer.Thalassa.SpeechSynthesis;
//using StarmaidIntegrationComputer.Twitch;

//using TwitchLib.Api;
//using TwitchLib.Client;

//namespace StarmaidIntegrationComputer.Commands.Twitch
//{
//    internal class TurnOnShieldModeCommand : TwitchCommandBase
//    {
//        public TurnOnShieldModeCommand(ILogger<CommandBase> logger, SpeechComputer speechComputer, TwitchSensitiveSettings twitchSensitiveSettings, TwitchClient chatbot, LiveAuthorizationInfo liveAuthorizationInfo, TwitchAPI twitchApi)
//            : base(logger, speechComputer, Enums.TwitchStateToValidate.ChatbotAndApi, liveAuthorizationInfo, twitchSensitiveSettings, twitchApi, chatbot)
//        {
//        }

//        protected override async Task PerformCommandAsync()
//        {
//            //TODO: Consider replacing the "flies by {TurnOnShieldModeTarget}" with a nickname, if one is set later!

//            TurnOnShieldModeCommandState state = await GetLastCategory();

//            string[] categoriesThatAreVerbs = { "Just Chatting" };

//            if (!categoriesThatAreVerbs.Contains(state.LastCategoryName))
//            {
//                state.LastCategoryName = $"playing {state.LastCategoryName}";
//            }

//            chatbot.SendMessage(twitchSensitiveSettings.TwitchChatbotChannelName, $"Everyone check it out as the Starmaid flies by @{TurnOnShieldModeTarget}, at https://twitch.tv/{TurnOnShieldModeTarget} where they were last {state.LastCategoryName}");

//            //TODO: Once the enum list for the scopes includes the right scope for this, this is how
//            //  we do a /TurnOnShieldMode! :D
//            //await twitchApi.Helix.Chat.SendTurnOnShieldModeAsync(liveAuthorizationInfo.BroadcasterId, state.RecipientBroadcasterId, settings.TwitchClientId, liveAuthorizationInfo.AccessToken.Token);
//        }

//        private async Task<TurnOnShieldModeCommandState> GetLastCategory()
//        {
//            TurnOnShieldModeCommandState state = new TurnOnShieldModeCommandState();
//#warning Error handling here if we are shouting out a non name - it's a BadRequestException.
//            var targets = await twitchApi.Helix.Users.GetUsersAsync(logins: new List<string> { TurnOnShieldModeTarget });

//            if (targets.Users.Count() == 0)
//            {
//                state.LastCategoryName = "<User not found, can't look up category>";
//                return state;
//            }

//            state.RecipientBroadcasterId = targets.Users.First().Id;

//            var channelInformationResponse = await twitchApi.Helix.Channels.GetChannelInformationAsync(state.RecipientBroadcasterId);

//            if (channelInformationResponse.Data.Length == 0)
//            {
//                state.LastCategoryName = "Found the user but not their channel, can't look up category";
//                return state;
//            }

//            state.LastCategoryName = channelInformationResponse.Data.First().GameName;
//            return state;
//        }
//    }
//}
