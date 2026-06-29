using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using StarmaidIntegrationComputer.Common.DataStructures.Audience;
using StarmaidIntegrationComputer.Thalassa.SpeechSynthesis;
using StarmaidIntegrationComputer.Twitch.ExternalApiClients.Pronouns;

namespace StarmaidIntegrationComputer.Commands.Twitch
{
    internal class SayRaiderListCommand : CommandBase
    {
        public AudienceRegistry AudienceRegistry { get; }
        private readonly PronounLookupService pronounLookupService;

        public SayRaiderListCommand(ILogger<CommandBase> logger, SpeechComputer speechComputer, AudienceRegistry audienceRegistry, PronounLookupService pronounLookupService) : base(logger, speechComputer)
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

            List<string> formattedRaiders = new List<string>();
            foreach (RaiderInfo raider in AudienceRegistry.Raiders)
            {
                string pronounDisplay = await pronounLookupService.GetPronounLabelOrEmptyString(raider.RaiderName);
                formattedRaiders.Add($"{raider.RaiderName}{pronounDisplay}");
            }

            string allRaiders;
            if (formattedRaiders.Count > 1)
            {
                string allButLast = string.Join(", ", formattedRaiders.Take(formattedRaiders.Count - 1));
                allRaiders = $"{allButLast}, and {formattedRaiders.Last()}";
            }
            else
            {
                allRaiders = formattedRaiders.First();
            }

            string sIfPlural = AudienceRegistry.Raiders.Count != 1 ? "s" : "";
            speechComputer.Speak($"{AudienceRegistry.Raiders.Count} raider{sIfPlural}: {allRaiders}");
        }
    }
}
