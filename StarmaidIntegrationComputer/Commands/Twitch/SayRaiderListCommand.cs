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
        public StarmaidStateBag StateBag { get; }

        public SayRaiderListCommand(ILogger<CommandBase> logger, SpeechComputer speechComputer, StarmaidStateBag stateBag) : base(logger, speechComputer)
        {
            StateBag = stateBag;
        }

        protected override Task PerformCommandAsync()
        {
            if (!StateBag.Raiders.Any())
            {
                speechComputer.Speak($"No raiders found.");
                return Task.CompletedTask;
            }

            IEnumerable<string> allRaidersButTheLastOne = StateBag.Raiders.Take(StateBag.Raiders.Count() - 1).Select(raider => raider.RaiderName);
            string allRaiders = string.Join(", ", allRaidersButTheLastOne);
            if (StateBag.Raiders.Count() > 1)
            {
                allRaiders += ", and ";
            }
            allRaiders = allRaiders += StateBag.Raiders.Last().RaiderName;
            string sIfPlural = StateBag.Raiders.Count() != 1 ? "s" : "";

            speechComputer.Speak($"{StateBag.Raiders.Count()} raider{sIfPlural}: {allRaiders}");
            return Task.CompletedTask;
        }
    }
}
