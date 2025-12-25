using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using StarmaidIntegrationComputer.Common.DataStructures.StarmaidState;
using StarmaidIntegrationComputer.Thalassa.SpeechSynthesis;

namespace StarmaidIntegrationComputer.Commands.Twitch
{
    internal class SayRaiderListCommand : CommandBase
    {
        public AudienceRegistry AudienceRegistry { get; }

        public SayRaiderListCommand(ILogger<CommandBase> logger, SpeechComputer speechComputer, AudienceRegistry audienceRegistry) : base(logger, speechComputer)
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

            IEnumerable<string> allRaidersButTheLastOne = AudienceRegistry.Raiders.Take(AudienceRegistry.Raiders.Count() - 1).Select(raider => raider.RaiderName);
            string allRaiders = string.Join(", ", allRaidersButTheLastOne);
            if (AudienceRegistry.Raiders.Count() > 1)
            {
                allRaiders += ", and ";
            }
            allRaiders = allRaiders += AudienceRegistry.Raiders.Last().RaiderName;
            string sIfPlural = AudienceRegistry.Raiders.Count() != 1 ? "s" : "";

            speechComputer.Speak($"{AudienceRegistry.Raiders.Count()} raider{sIfPlural}: {allRaiders}");
            return Task.CompletedTask;
        }
    }
}
