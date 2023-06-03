using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using StarmaidIntegrationComputer.StarmaidSettings;
using StarmaidIntegrationComputer.Thalassa.SpeechSynthesis;

using TwitchLib.Api;
using TwitchLib.Client;

namespace StarmaidIntegrationComputer.Commands.Twitch
{
    internal class ShoutoutCommand : TwitchCommandBase
    {
        private readonly Settings settings;

        public string ShoutoutTarget { get; private set; }

        public ShoutoutCommand(ILogger<CommandBase> logger, SpeechComputer speechComputer, Settings settings, TwitchClient chatbot, TwitchAPI twitchApi, string target) : base(logger, speechComputer, Enums.TwitchStateToValidate.ChatbotAndApi, twitchApi, chatbot)
        {
            this.settings = settings;
            this.ShoutoutTarget = target;
        }

        protected override async Task PerformCommand()
        {
            //TODO: Consider replacing the "flies by {ShoutoutTarget}" with a nickname, if one is set later!

            string lastCategory = await GetLastCategory();

            string[] categoriesThatAreVerbs = { "Just Chatting" };

            if (!categoriesThatAreVerbs.Contains(lastCategory))
            {
                lastCategory = $"playing {lastCategory}";
            }

            //TODO: ??? why does the chatbot's Joined Channels list empty??  It's clearly still in there!
            if (chatbot.JoinedChannels.Count() == 0)
            {
                chatbot.JoinChannel(settings.TwitchChatbotChannelName);
            }

            chatbot.SendMessage(settings.TwitchChatbotChannelName, $"Everyone check it out as the Starmaid flies by {ShoutoutTarget}, at https://twitch.tv/{ShoutoutTarget} where they were last {lastCategory}");
            
            //TODO: Use shoutout API call with a newer version of TwitchLib.

            //chatbot.SendMessage(settings.TwitchChatbotChannelName, $"/shoutout {ShoutoutTarget}");
        }

        private async Task<string> GetLastCategory()
        {
            var targets = await twitchApi.Helix.Users.GetUsersAsync(logins: new List<string> { ShoutoutTarget });

            if (targets.Users.Count() == 0)
            {
                return "<User not found, can't look up category>";
            }

            var broadcasterId = targets.Users.First().Id;

            var channelInformationResponse = await twitchApi.Helix.Channels.GetChannelInformationAsync(broadcasterId);

            if (channelInformationResponse.Data.Length == 0)
            {
                return "Found the user but not their channel, can't look up category";
            }

            var category = channelInformationResponse.Data.First().GameName;
            return category;
        }
    }
}
