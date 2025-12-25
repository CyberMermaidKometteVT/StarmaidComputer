using Microsoft.Extensions.Logging;

using StarmaidIntegrationComputer.Common.Settings;
using StarmaidIntegrationComputer.StarmaidSettings;
using StarmaidIntegrationComputer.Thalassa.Settings;

namespace StarmaidIntegrationComputer.Thalassa.WakeWordProcessor
{
    public class WakeWordProcessorFactory : IWakeWordProcessorFactory
    {
        private readonly ILogger<WakeWordProcessorFactory> factoryLogger;
        private readonly ThalassaSettings thalassaSettings;
        private readonly ThalassaSensitiveSettings sensitiveSettings;
        private readonly ILogger<WakeWordProcessorBase> processorLogger;
        private readonly StreamerProfileSettings streamerProfileSettings;

        public WakeWordProcessorFactory(ILogger<WakeWordProcessorFactory> factoryLogger, ThalassaSettings thalassaSettings, ThalassaSensitiveSettings sensitiveSettings, ILogger<WakeWordProcessorBase> processorLogger, StreamerProfileSettings streamerProfileSettings)
        {
            this.factoryLogger = factoryLogger;
            this.thalassaSettings = thalassaSettings;
            this.sensitiveSettings = sensitiveSettings;
            this.processorLogger = processorLogger;
            this.streamerProfileSettings = streamerProfileSettings;
            Processor = Build();
        }

        public WakeWordProcessorBase Processor { get; }

        private WakeWordProcessorBase Build()
        {
            if (thalassaSettings.WakeWordSelectedInterpreter == WakeWordProcessorType.Porcupine)
            {
                if (sensitiveSettings.PorcupineKeywordFilePaths != null && sensitiveSettings.PorcupineKeywordFilePaths.Length != 0 && !sensitiveSettings.PorcupineKeywordFilePaths.Any(keywordFilePath => !File.Exists(keywordFilePath)))
                {
                    try
                    {
                        return new WakeWordProcessorPorcupine(processorLogger, streamerProfileSettings, sensitiveSettings.PorcupineAccessKey, sensitiveSettings.PorcupineKeywordFilePaths);
                    }
                    catch (Exception ex)
                    {
                        factoryLogger.LogError($"Failed to load Porcupine wake word processor. Error: {ex.Message}; Stack: {ex.StackTrace}");
                    }
                }
            }

            if (thalassaSettings.WakeWordSelectedInterpreter == WakeWordProcessorType.WindowsVoice)
            {
                return new MicrosoftWakeWordProcessor(processorLogger, streamerProfileSettings, thalassaSettings);
            }

            string invalidSelectionErrorMessage = $"Wake word interpreter type '{thalassaSettings.WakeWordSelectedInterpreter}' in Thalassa Settings is blank or invalid.";

            factoryLogger.LogError(invalidSelectionErrorMessage);

            //Bubble the error up - probably crash the application - if we aren't sure what they want to do for the wake word.
            throw new Exception(invalidSelectionErrorMessage);
        }
    }
}
