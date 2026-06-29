using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using StarmaidIntegrationComputer.Common.DataStructures.Audience;
using StarmaidIntegrationComputer.Thalassa.SpeechSynthesis;
using StarmaidIntegrationComputer.Twitch.ExternalApiClients.Pronouns;

namespace StarmaidIntegrationComputer.Commands.Twitch
{
    internal class SayLastRaiderCommand : CommandBase
    {
        public AudienceRegistry AudienceRegistry { get; }
        private readonly PronounLookupService pronounLookupService;

        public SayLastRaiderCommand(ILogger<CommandBase> logger, SpeechComputer speechComputer, AudienceRegistry audienceRegistry, PronounLookupService pronounLookupService) : base(logger, speechComputer)
        {
            AudienceRegistry = audienceRegistry;
            this.pronounLookupService = pronounLookupService;
        }

        protected override async Task PerformCommandAsync()
        {
            if (!AudienceRegistry.Raiders.Any())
            {
                speechComputer.Speak($"No raiders found.");
                return;
            }

            RaiderInfo raiderInfo = AudienceRegistry.Raiders.First();
            string lastShoutedOutDescription = "Not shouted out.";
            if (raiderInfo.LastShoutedOut != null)
            {
                int minutesAgo = (DateTime.Now - raiderInfo.LastShoutedOut).Value.Minutes;
                lastShoutedOutDescription = $"Last shouted out {minutesAgo} minutes ago.";
            }

            string pronounDisplay = await pronounLookupService.GetPronounLabelOrEmptyString(raiderInfo.RaiderName);
            speechComputer.Speak($"Last raider: {raiderInfo.RaiderName}{pronounDisplay} at {raiderInfo.RaidTime.ToString("hh mm tt")}.  {lastShoutedOutDescription}");
        }
    }
}
