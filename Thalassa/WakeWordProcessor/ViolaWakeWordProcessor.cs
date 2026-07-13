using Microsoft.Extensions.Logging;

using NAudio.Wave;

using StarmaidIntegrationComputer.Common.Settings;
using StarmaidIntegrationComputer.Thalassa.Settings;
using StarmaidIntegrationComputer.Thalassa.WakeWordProcessor.OnnxWakeWord;

namespace StarmaidIntegrationComputer.Thalassa.WakeWordProcessor
{
    /// <summary>
    /// Wake word/command listener trained via ViolaWake (https://violawake.com) on real recordings
    /// of the streamer's own voice, running locally through Microsoft.ML.OnnxRuntime instead of a
    /// Python subprocess, since ViolaWake only ships a Python SDK. Each configured phrase
    /// (wake word, cancel-listening, abort-command) is its own trained classifier head, sharing one
    /// melspectrogram/embedding pass for efficiency.
    /// </summary>
    public class ViolaWakeWordProcessor : WakeWordProcessorBase
    {
        private const int DebounceMilliseconds = 1250;

        private readonly ILogger<WakeWordProcessorBase> logger;
        private readonly ViolaWakeSettings violaWakeSettings;
        private readonly OnnxWakeWordPipeline pipeline;
        private readonly WaveInEvent waveIn = new();

        private readonly Dictionary<string, DateTime> lastDetectionByClassifier = new();

        public ViolaWakeWordProcessor(ILogger<WakeWordProcessorBase> logger, StreamerProfileSettings streamerProfileSettings, ViolaWakeSettings violaWakeSettings, OnnxWakeWordPipeline pipeline)
            : base(logger, streamerProfileSettings)
        {
            this.logger = logger;
            this.violaWakeSettings = violaWakeSettings;
            this.pipeline = pipeline;

            waveIn.WaveFormat = new WaveFormat(16000, 16, 1);
            waveIn.DataAvailable += WaveIn_DataAvailable;
        }

        public override void StartListening()
        {
            waveIn.StartRecording();
            IsListening = true;
        }

        public override void StopListening()
        {
            IsListening = false;
            waveIn.StopRecording();
            logger.LogInformation("Sleeping, not listening for the wake word...");
        }

        private void WaveIn_DataAvailable(object? sender, WaveInEventArgs e)
        {
            if (!IsListening)
            {
                return;
            }

            int sampleCount = e.BytesRecorded / 2;
            short[] samples = new short[sampleCount];
            Buffer.BlockCopy(e.Buffer, 0, samples, 0, sampleCount * 2);

            IReadOnlyDictionary<string, float> confidences = pipeline.ProcessAudio(samples);

            TryHandleDetection(confidences, OnnxWakeWordPipeline.WakeWordClassifierName, violaWakeSettings.WakeWordConfidenceThreshold, OnWakeWordHeard);
            TryHandleDetection(confidences, OnnxWakeWordPipeline.CancelListeningClassifierName, violaWakeSettings.CancelListeningConfidenceThreshold, OnCancelListeningHeard);
            TryHandleDetection(confidences, OnnxWakeWordPipeline.AbortCommandClassifierName, violaWakeSettings.AbortCommandConfidenceThreshold, OnAbortCommandHeard);
        }

        private void TryHandleDetection(IReadOnlyDictionary<string, float> confidences, string classifierName, float threshold, Action onDetected)
        {
            if (!confidences.TryGetValue(classifierName, out float confidence))
            {
                return;
            }

            if (confidence <= threshold)
            {
                logger.LogInformation($"'{classifierName}' wake word REJECTING with a confidence threshold of (confidence={confidence}/{threshold})");
                return;
            }

            DateTime lastDetection = lastDetectionByClassifier.GetValueOrDefault(classifierName, DateTime.MinValue);
            if (DateTime.Now - lastDetection <= TimeSpan.FromMilliseconds(DebounceMilliseconds))
            {
                return;
            }

            lastDetectionByClassifier[classifierName] = DateTime.Now;
                logger.LogInformation($"'{classifierName}' wake word MATHCED with a confidence threshold of (confidence={confidence}/{threshold})");
            logger.LogInformation($"'{classifierName}' detected by {GetType().Name}! (confidence={confidence})");
            onDetected();
        }

        public override void Dispose()
        {
            waveIn.Dispose();
            pipeline.Dispose();
        }
    }
}
