using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using StarmaidIntegrationComputer.StarmaidSettings;
using StarmaidIntegrationComputer.Thalassa.SpeechSynthesis;
using StarmaidIntegrationComputer.Twitch;

using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Moderation.BanUser;
using TwitchLib.Client;

namespace StarmaidIntegrationComputer.Commands.Twitch
{
    internal class TimeoutCommand : TwitchCommandBase
    {
        private string timeoutTarget;

        private readonly int durationInSeconds;

        const string timeoutReason = "Because Komette said so, and Thalassa obeyed";

        public TimeoutCommand(ILogger<CommandBase> logger, SpeechComputer speechComputer, TwitchSensitiveSettings twitchSensitiveSettings, TwitchClient chatbot, LiveAuthorizationInfo liveAuthorizationInfo, TwitchAPI twitchApi, string target, int durationInSeconds) : base(logger, speechComputer, Enums.TwitchStateToValidate.ChatbotAndApi, liveAuthorizationInfo, twitchSensitiveSettings, twitchApi, chatbot)
        {
            this.timeoutTarget = target;
            this.durationInSeconds = durationInSeconds;
        }


        protected override async Task PerformCommandAsync()
        {
            string userId = await GetUserId(timeoutTarget);

            if (String.IsNullOrWhiteSpace(userId))
            {
                speechComputer.Speak($"Unable to time out {timeoutTarget ?? "<no user specified>"}, user not found!");
                return;
            }

            var request = new BanUserRequest { Duration = durationInSeconds, Reason = timeoutReason, UserId = userId };
            var response = await twitchApi.Helix.Moderation.BanUserAsync(liveAuthorizationInfo.StreamerBroadcasterId, liveAuthorizationInfo.ThalassaUserId, request, liveAuthorizationInfo.AccessToken.Token);

            speechComputer.Speak($"Banned {response.Data.Count()} users.");
        }
    }
}
