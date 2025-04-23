using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using StarmaidIntegrationComputer.Common.Settings;
using StarmaidIntegrationComputer.StarmaidSettings;
using StarmaidIntegrationComputer.Thalassa.SpeechSynthesis;
using StarmaidIntegrationComputer.Twitch;

using TwitchLib.Api;
using TwitchLib.Client;

namespace StarmaidIntegrationComputer.Commands.Twitch
{
    internal class SendChatMessageCommand : TwitchCommandBase
    {
        private readonly StreamerProfileSettings ProfileSettings;
        public string ChatMessage { get; }
        public SendChatMessageCommand(ILogger<CommandBase> logger, SpeechComputer speechComputer, TwitchSensitiveSettings twitchSensitiveSettings, LiveAuthorizationInfo liveAuthorizationInfo, TwitchAPI twitchApi, TwitchClient chatbot, StreamerProfileSettings profileSettings, string chatMessage)
            : base(logger, speechComputer, Enums.TwitchStateToValidate.ChatbotAndApi, liveAuthorizationInfo, twitchSensitiveSettings, twitchApi, chatbot)
        {
            this.ProfileSettings = profileSettings;
            this.ChatMessage = chatMessage;
        }

        protected override Task PerformCommandAsync()
        {
            //TODO: Consider replacing the "flies by {ShoutoutTarget}" with a nickname, if one is set later!

            chatbot.SendMessage(channel: twitchSensitiveSettings.TwitchChatbotChannelName,
                message: $"{this.ChatMessage}");

            return Task.CompletedTask;
        }

    }
}
