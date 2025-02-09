using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using StarmaidIntegrationComputer.StarmaidSettings;
using StarmaidIntegrationComputer.Thalassa.SpeechSynthesis;
using StarmaidIntegrationComputer.Twitch;

using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Moderation.BanUser;

namespace StarmaidIntegrationComputer.Commands.Twitch
{
    internal class TimeoutCommand : TwitchCommandBase
    {
        private string timeoutTarget;

        private readonly int durationInSeconds;
        private string? timeoutReason;

        public TimeoutCommand(ILogger<CommandBase> logger, SpeechComputer speechComputer, TwitchSensitiveSettings twitchSensitiveSettings, LiveAuthorizationInfo liveAuthorizationInfo, TwitchAPI twitchApi, string target, int durationInSeconds, string? timeoutReason = null) : base(logger, speechComputer, Enums.TwitchStateToValidate.Api, liveAuthorizationInfo, twitchSensitiveSettings, twitchApi)
        {
            this.timeoutTarget = target;
            this.durationInSeconds = durationInSeconds;
            this.timeoutReason = timeoutReason;
        }


        protected override async Task PerformCommandAsync()
        {
            if (String.IsNullOrWhiteSpace(timeoutReason))
            {
                timeoutReason = $"Because {twitchSensitiveSettings.TwitchChatbotChannelName} said so, and {twitchSensitiveSettings.TwitchChatbotUsername} obeyed";
            }

            string userId = await GetUserId(timeoutTarget);

            if (String.IsNullOrWhiteSpace(userId))
            {
                speechComputer.Speak($"Unable to time out {timeoutTarget ?? "<no user specified>"}, user not found!");
                return;
            }

            var request = new BanUserRequest { Duration = durationInSeconds, Reason = timeoutReason, UserId = userId };
            var response = await twitchApi.Helix.Moderation.BanUserAsync(liveAuthorizationInfo.StreamerBroadcasterId, liveAuthorizationInfo.ThalassaUserId, request, liveAuthorizationInfo.AccessToken.Token);

            speechComputer.Speak($"Timed out {response.Data.Count()} users.");
        }
    }
}
