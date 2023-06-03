using Microsoft.Extensions.Logging;

using StarmaidIntegrationComputer.Commands.Twitch.Enums;
using StarmaidIntegrationComputer.Thalassa.SpeechSynthesis;

using TwitchLib.Api;
using TwitchLib.Client;

namespace StarmaidIntegrationComputer.Commands.Twitch
{
    internal abstract class TwitchCommandBase : CommandBase
    {
        protected readonly TwitchAPI? twitchApi;
        protected readonly TwitchClient? chatbot;
        public TwitchStateToValidate stateToValidate;
        protected TwitchCommandBase(ILogger<CommandBase> logger, SpeechComputer speechComputer, TwitchStateToValidate stateToValidate, TwitchAPI? twitchApi = null, TwitchClient? chatbot = null) : base(logger, speechComputer)
        {
            this.stateToValidate = stateToValidate;
            this.twitchApi = twitchApi;
            this.chatbot = chatbot;
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
