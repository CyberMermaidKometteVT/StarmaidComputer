using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using StarmaidIntegrationComputer.Commands.Twitch.Enums;
using StarmaidIntegrationComputer.StarmaidSettings;
using StarmaidIntegrationComputer.Thalassa.Settings;
using StarmaidIntegrationComputer.Thalassa.SpeechSynthesis;
using StarmaidIntegrationComputer.Twitch;

using TwitchLib.Client;

namespace StarmaidIntegrationComputer.Commands.Twitch
{
    internal class SendCannedMessageToChatCommand : TwitchCommandBase
    {
        private readonly ThalassaSettings thalassaSettings;

        public SendCannedMessageToChatCommand(ILogger<CommandBase> logger, ThalassaSettings thalassaSettings, TwitchSensitiveSettings twitchSensitiveSettings, SpeechComputer speechComputer, TwitchStateToValidate stateToValidate, LiveAuthorizationInfo liveAuthorizationInfo, TwitchClient chatbot) : base(logger, speechComputer, stateToValidate, liveAuthorizationInfo, twitchSensitiveSettings, chatbot: chatbot)
        {
            this.thalassaSettings = thalassaSettings;
        }
        protected override Task PerformCommandAsync()
        {
            chatbot.SendMessage(twitchSensitiveSettings.TwitchChatbotChannelName, thalassaSettings.CannedMessageText);
            return Task.CompletedTask;
        }
    }
}
