using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using StarmaidIntegrationComputer.Thalassa.SpeechSynthesis;

namespace StarmaidIntegrationComputer.Commands
{
    internal class FailedCommand : CommandBase
    {
        private readonly string commandFailedDescription;
        public FailedCommand(ILogger<CommandBase> logger, SpeechComputer speechComputer, string commandFailedDescription ) : base(logger, speechComputer)
        {
            this.commandFailedDescription = commandFailedDescription;
        }

        protected override Task PerformCommandAsync()
        {
            throw new CommandFailedException(commandFailedDescription);
        }
    }
}
