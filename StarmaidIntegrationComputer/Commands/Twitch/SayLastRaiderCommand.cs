using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using StarmaidIntegrationComputer.Common.DataStructures.StarmaidState;
using StarmaidIntegrationComputer.Thalassa.SpeechSynthesis;

namespace StarmaidIntegrationComputer.Commands.Twitch
{
    internal class SayLastRaiderCommand : CommandBase
    {
        public AudienceRegistry AudienceRegistry { get; }

        public SayLastRaiderCommand(ILogger<CommandBase> logger, SpeechComputer speechComputer, AudienceRegistry audienceRegistry) : base(logger, speechComputer)
        {
            AudienceRegistry = audienceRegistry;
        }

        protected override Task PerformCommandAsync()
        {
            if (!AudienceRegistry.Raiders.Any())
            {
                speechComputer.Speak($"No raiders found.");
                return Task.CompletedTask;
            }

            RaiderInfo raiderInfo = AudienceRegistry.Raiders.First();
            string lastShoutedOutDescription = "Not shouted out.";
            if (raiderInfo.LastShoutedOut != null)
            {
                int minutesAgo = (DateTime.Now - raiderInfo.LastShoutedOut).Value.Minutes;
                lastShoutedOutDescription = $"Last shouted out {minutesAgo} minutes ago.";
            }

            speechComputer.Speak($"Last raider: {raiderInfo.RaiderName} at {raiderInfo.RaidTime.ToString("hh mm tt")}.  {lastShoutedOutDescription}");
            return Task.CompletedTask;
        }
    }
}
