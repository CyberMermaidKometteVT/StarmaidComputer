using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using StarmaidIntegrationComputer.StarmaidSettings;
using StarmaidIntegrationComputer.Thalassa.SpeechSynthesis;
using StarmaidIntegrationComputer.Twitch;

using TwitchLib.Api;
using TwitchLib.Client;

namespace StarmaidIntegrationComputer.Commands.Twitch
{
    internal class ResetKruizControlCommand : TwitchCommandBase
    {
        public ResetKruizControlCommand(ILogger<CommandBase> logger, SpeechComputer speechComputer, TwitchSensitiveSettings twitchSensitiveSettings, LiveAuthorizationInfo liveAuthorizationInfo, TwitchAPI twitchApi, TwitchClient chatbot)
            : base(logger, speechComputer, Enums.TwitchStateToValidate.Api, liveAuthorizationInfo, twitchSensitiveSettings, twitchApi, chatbot)
        {
        }

        protected override Task PerformCommandAsync()
        {
            chatbot.SendMessage(twitchSensitiveSettings.TwitchChatbotChannelName, "!kcreset");

            return Task.CompletedTask;
        }
    }
}
