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
    internal class TurnOnShieldModeCommand : TwitchCommandBase
    {
        public TurnOnShieldModeCommand(ILogger<CommandBase> logger, SpeechComputer speechComputer, TwitchSensitiveSettings twitchSensitiveSettings, TwitchClient chatbot, LiveAuthorizationInfo liveAuthorizationInfo, TwitchAPI twitchApi)
            : base(logger, speechComputer, Enums.TwitchStateToValidate.ChatbotAndApi, liveAuthorizationInfo, twitchSensitiveSettings, twitchApi, chatbot)
        {
        }

        protected override async Task PerformCommandAsync()
        {

        }
    }
}
