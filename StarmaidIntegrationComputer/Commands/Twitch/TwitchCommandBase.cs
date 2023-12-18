using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using StarmaidIntegrationComputer.Commands.Twitch.Enums;
using StarmaidIntegrationComputer.StarmaidSettings;
using StarmaidIntegrationComputer.Thalassa.SpeechSynthesis;
using StarmaidIntegrationComputer.Twitch;

using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using TwitchLib.Client;

namespace StarmaidIntegrationComputer.Commands.Twitch
{
    internal abstract class TwitchCommandBase : CommandBase
    {
        protected readonly TwitchAPI? twitchApi;
        protected readonly TwitchClient? chatbot;
        protected readonly LiveAuthorizationInfo liveAuthorizationInfo;
        protected readonly TwitchSensitiveSettings twitchSensitiveSettings;
        public TwitchStateToValidate stateToValidate;
        protected TwitchCommandBase(ILogger<CommandBase> logger, SpeechComputer speechComputer, TwitchStateToValidate stateToValidate, LiveAuthorizationInfo liveAuthorizationInfo, TwitchSensitiveSettings twitchSensitiveSettings, TwitchAPI? twitchApi = null, TwitchClient? chatbot = null) : base(logger, speechComputer)
        {
            this.stateToValidate = stateToValidate;
            this.twitchApi = twitchApi;
            this.chatbot = chatbot;
            this.liveAuthorizationInfo = liveAuthorizationInfo;
            this.twitchSensitiveSettings = twitchSensitiveSettings;
        }

        protected override bool ValidateState()
        {
            if ((stateToValidate & TwitchStateToValidate.Api) == TwitchStateToValidate.Api)
            {
                if (twitchApi == null)
                {
                    speechComputer.Speak($"Twitch API not set up!  Unable to execute command {this.GetType().Name}");
                    return false;
                }
                //TODO: I'm not actually sure how to validate that the API is connectable tbh?  Maybe stick the auth token somewhere accessible?
            }
            if ((stateToValidate & TwitchStateToValidate.Chatbot) == TwitchStateToValidate.Chatbot)
            {
                if (chatbot == null || !chatbot.IsConnected)
                {
                    speechComputer.Speak($"Twitch chatbot not yet running!  Unable to execute command {this.GetType().Name}");
                    return false;
                }

                ValidateInChannel();
            }


            return base.ValidateState();
        }

        protected void ValidateInChannel()
        {
            //TODO: ??? why does the chatbot's Joined Channels list empty??  It's clearly still in there!
            if (chatbot.JoinedChannels.Count() == 0)
            {
                chatbot.JoinChannel(twitchSensitiveSettings.TwitchChatbotChannelName);
            }
        }

        //Should this live outside of the command system?
        protected async Task<User[]> GetUsersAsync(params string[] userNames)
        {
            var response = await twitchApi.Helix.Users.GetUsersAsync(logins: new List<string>(userNames));
            return response.Users;
        }

        protected async Task<User?> GetUserAsync(string userName)
        {
            return (await GetUsersAsync(userName))?.FirstOrDefault();
        }

        protected async Task<string> GetUserId(string userName)
        {
            return (await GetUserAsync(userName)).Id;
        }
    }
}
