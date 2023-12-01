using Microsoft.Extensions.Logging;

using StarmaidIntegrationComputer.Commands.Twitch.Enums;
using StarmaidIntegrationComputer.StarmaidSettings;
using StarmaidIntegrationComputer.Thalassa.SpeechSynthesis;
using StarmaidIntegrationComputer.Twitch;

using TwitchLib.Api;
using TwitchLib.Client;

namespace StarmaidIntegrationComputer.Commands.Twitch
{
    internal abstract class TwitchCommandBase : CommandBase
    {
        protected readonly TwitchAPI? twitchApi;
        protected readonly TwitchClient? chatbot;
        protected readonly LiveAuthorizationInfo liveAuthorizationInfo;

        public TwitchStateToValidate stateToValidate;
        protected TwitchCommandBase(ILogger<CommandBase> logger, SpeechComputer speechComputer, TwitchStateToValidate stateToValidate, LiveAuthorizationInfo liveAuthorizationInfo, TwitchAPI? twitchApi = null, TwitchClient? chatbot = null) : base(logger, speechComputer)
        {
            this.stateToValidate = stateToValidate;
            this.twitchApi = twitchApi;
            this.chatbot = chatbot;
            this.liveAuthorizationInfo = liveAuthorizationInfo;
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
            }

            return base.ValidateState();
        }
    }
}
