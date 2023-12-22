
using Microsoft.Extensions.Logging;

using StarmaidIntegrationComputer.Common.Settings;
using StarmaidIntegrationComputer.Thalassa;
using StarmaidIntegrationComputer.Thalassa.SpeechSynthesis;

namespace StarmaidIntegrationComputer.UdpThalassaControl
{
    public class RemoteThalassaControlInterpreter
    {
        private readonly ILogger<RemoteThalassaControlInterpreter> logger;
        private readonly string aiName;
        private readonly ThalassaCore thalassaCore;
        private readonly SpeechComputer speechComputer;

        public RemoteThalassaControlInterpreter(ILoggerFactory loggerFactory, StreamerProfileSettings streamerProfileSettings, ThalassaCore thalassaCore, SpeechComputer speechComputer)
        {
            this.logger = loggerFactory.CreateLogger<RemoteThalassaControlInterpreter>();
            this.aiName = streamerProfileSettings?.AiName ?? "Your AI";
            this.thalassaCore = thalassaCore;
            this.speechComputer = speechComputer;
        }

        public void Interpret(string controlCommand)
        {
            logger.LogDebug($"{aiName} control command received: {controlCommand}");
            switch (controlCommand)
            {
                case ThalassaControlCommands.SLEEP:
                    logger.LogInformation($"Sleep remote command received! Sleeping!");
                    thalassaCore.StopListening();
                    break;

                case ThalassaControlCommands.WAKE:
                    logger.LogInformation($"Wake remote command received! Waking!");
                    thalassaCore.StartListening();
                    break;

                case ThalassaControlCommands.INPUT_OVER:
                    logger.LogInformation($"Input Over command received! Concluding listening!");
                    thalassaCore.ConcludeCurrentListening();
                    break;

                case ThalassaControlCommands.ABORT_COMMAND:
                    logger.LogInformation($"'Abort Command' command received! Aborting!");
                    thalassaCore.AbortCommandIssued();
                    break;

                case ThalassaControlCommands.SHUT_UP:
                    logger.LogInformation($"Cancel listening remote command received! Stopping listening!");
                    thalassaCore.CancelSpeech();
                    break;

                case ThalassaControlCommands.NOT_TALKING_TO_YOU:
                    logger.LogInformation($"Cancel listening remote command received! Stopping listening!");
                    thalassaCore.AbortCurrentListening();
                    break;

                default:
                    string errorMessage = $"ERROR: Unexpected {aiName} control command received: {controlCommand}. You probably have your external command sending tool misconfigured!";
                    logger.LogError(errorMessage);
                    speechComputer.Speak(errorMessage);
                    break;
            }
        }
    }
}
