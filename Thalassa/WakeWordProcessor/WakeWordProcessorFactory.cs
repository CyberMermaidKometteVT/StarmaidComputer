using Microsoft.Extensions.Logging;

using StarmaidIntegrationComputer.Common.Assets;
using StarmaidIntegrationComputer.Common.Settings;
using StarmaidIntegrationComputer.StarmaidSettings;
using StarmaidIntegrationComputer.Thalassa.Settings;
using StarmaidIntegrationComputer.Thalassa.WakeWordProcessor.OnnxWakeWord;

namespace StarmaidIntegrationComputer.Thalassa.WakeWordProcessor
{
    public class WakeWordProcessorFactory : IWakeWordProcessorFactory
    {
        private readonly ILogger<WakeWordProcessorFactory> factoryLogger;
        private readonly ThalassaSettings thalassaSettings;
        private readonly ThalassaSensitiveSettings sensitiveSettings;
        private readonly ViolaWakeSettings violaWakeSettings;
        private readonly AssetDownloader assetDownloader;
        private readonly ILogger<WakeWordProcessorBase> processorLogger;
        private readonly ILogger<OnnxWakeWordPipeline> pipelineLogger;
        private readonly StreamerProfileSettings streamerProfileSettings;

        public WakeWordProcessorFactory(ILogger<WakeWordProcessorFactory> factoryLogger, ThalassaSettings thalassaSettings, ThalassaSensitiveSettings sensitiveSettings, ViolaWakeSettings violaWakeSettings, AssetDownloader assetDownloader, ILogger<WakeWordProcessorBase> processorLogger, ILogger<OnnxWakeWordPipeline> pipelineLogger, StreamerProfileSettings streamerProfileSettings)
        {
            this.factoryLogger = factoryLogger;
            this.thalassaSettings = thalassaSettings;
            this.sensitiveSettings = sensitiveSettings;
            this.violaWakeSettings = violaWakeSettings;
            this.assetDownloader = assetDownloader;
            this.processorLogger = processorLogger;
            this.pipelineLogger = pipelineLogger;
            this.streamerProfileSettings = streamerProfileSettings;
            Processor = Build();
        }

        public WakeWordProcessorBase Processor { get; }

        private WakeWordProcessorBase Build()
        {
            if (thalassaSettings.WakeWordSelectedInterpreter == WakeWordProcessorType.Porcupine)
            {
                if (thalassaSettings.PorcupineKeywordFilePaths != null && thalassaSettings.PorcupineKeywordFilePaths.Length != 0 && !thalassaSettings.PorcupineKeywordFilePaths.Any(keywordFilePath => !File.Exists(keywordFilePath)))
                {
                    try
                    {
                        return new WakeWordProcessorPorcupine(processorLogger, streamerProfileSettings, sensitiveSettings.PorcupineAccessKey, thalassaSettings.PorcupineKeywordFilePaths);
                    }
                    catch (Exception ex)
                    {
                        factoryLogger.LogError($"Failed to load Porcupine wake word processor. Error: {ex.Message}; Stack: {ex.StackTrace}");
                    }
                }
            }

            if (thalassaSettings.WakeWordSelectedInterpreter == WakeWordProcessorType.ViolaWake)
            {
                //OnnxWakeWordPipeline owns validating its own prerequisites (shared model downloads,
                //the required wake word classifier, optional cancel/abort classifiers) and throws a
                //clear, specific error for whichever one is wrong - caught and logged below.
                try
                {
                    OnnxWakeWordPipeline pipeline = new(pipelineLogger, assetDownloader, violaWakeSettings);
                    return new ViolaWakeWordProcessor(processorLogger, streamerProfileSettings, violaWakeSettings, pipeline);
                }
                catch (Exception ex)
                {
                    factoryLogger.LogError($"Failed to load ViolaWake wake word processor. Error: {ex.Message}; Stack: {ex.StackTrace}");
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
