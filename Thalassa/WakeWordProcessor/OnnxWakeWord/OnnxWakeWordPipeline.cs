using Microsoft.Extensions.Logging;

using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

using StarmaidIntegrationComputer.Common.Assets;
using StarmaidIntegrationComputer.Thalassa.Settings;

namespace StarmaidIntegrationComputer.Thalassa.WakeWordProcessor.OnnxWakeWord
{
    /// <summary>
    /// Reimplements the openWakeWord/ViolaWake audio pipeline (melspectrogram -> shared speech
    /// embedding -> trained classifier) with Microsoft.ML.OnnxRuntime, since neither project ships
    /// .NET bindings. Tensor shapes and preprocessing are taken from openWakeWord's utils.py
    /// (AudioFeatures class), which ViolaWake reuses unmodified for its shared melspectrogram and
    /// embedding stages. Multiple trained classifier heads (e.g. "Thalassa," "Abort Command") can
    /// share a single melspectrogram/embedding pass, since that's the expensive, phrase-agnostic part.
    /// Public because it (and ILogger&lt;OnnxWakeWordPipeline&gt;) appears in the public constructors
    /// of WakeWordProcessorFactory and ViolaWakeWordProcessor - C# requires that even within the same
    /// assembly.
    /// </summary>
    public sealed class OnnxWakeWordPipeline : IDisposable
    {
        public const string WakeWordClassifierName = "WakeWord";
        public const string CancelListeningClassifierName = "CancelListening";
        public const string AbortCommandClassifierName = "AbortCommand";

        private const int MelspecChunkSamples = 1280; // 80ms @ 16kHz; openWakeWord's streaming step size
        private const int MelspecChunkMilliseconds = 80; // MelspecChunkSamples expressed as time, for the timing diagnostic
        private const int MelspecContextSamples = 480; // openWakeWord's 160*3 - preceding audio replayed so boundary frames survive
        private const int MelFramesPerChunk = 8; // frames MelspecChunkSamples + MelspecContextSamples yields once context is primed
        private const int ChunksBetweenTimingReports = 100; // ~8 seconds of audio per timing diagnostic line
        private const int MelBins = 32;
        private const int EmbeddingWindowFrames = 76; // mel frames consumed per embedding inference
        private const int EmbeddingHopFrames = 8; // stride between successive embedding windows
        private const int EmbeddingDim = 96;
        private const int MelBufferMaxFrames = 200; // rolling history, comfortably above the 76-frame window

        private readonly ILogger<OnnxWakeWordPipeline> logger;
        private readonly InferenceSession melspectrogramSession;
        private readonly InferenceSession embeddingSession;
        private readonly string melInputName;
        private readonly string embeddingInputName;

        private readonly Dictionary<string, ClassifierSlot> classifiers;
        private readonly int embeddingBufferMaxLength;

        private readonly List<short> pendingRawSamples = new();
        private readonly List<short> melspecContext = new();
        private readonly List<float[]> melFrameBuffer = new();
        private readonly List<float[]> embeddingBuffer = new();

        private int framesSinceLastEmbedding;
        private bool hasWarnedAboutShortMelOutput;

        //Diagnostic only. The classifier heads consume a fixed-length run of embeddings as a time
        //series, and were trained on embeddings produced at one per audio chunk. If the melspectrogram
        //model emits fewer than EmbeddingHopFrames frames per chunk, embeddings come out slower and at
        //irregular spacing, which distorts that time series - so the actual rate is worth measuring
        //rather than assuming.
        private readonly Dictionary<int, int> melFramesPerChunkCounts = new();
        private int chunksSinceLastTimingReport;
        private int embeddingsSinceLastTimingReport;

        public OnnxWakeWordPipeline(ILogger<OnnxWakeWordPipeline> logger, AssetDownloader assetDownloader, ViolaWakeSettings violaWakeSettings)
        {
            this.logger = logger;

            assetDownloader.EnsureDownloaded(OpenWakeWordAssets.MelspectrogramLocalPath, OpenWakeWordAssets.Melspectrogram, logger);
            assetDownloader.EnsureDownloaded(OpenWakeWordAssets.EmbeddingModelLocalPath, OpenWakeWordAssets.EmbeddingModel, logger);

            melspectrogramSession = LoadModel(logger, "melspectrogram model", OpenWakeWordAssets.MelspectrogramLocalPath);
            embeddingSession = LoadModel(logger, "embedding model", OpenWakeWordAssets.EmbeddingModelLocalPath);

            melInputName = melspectrogramSession.InputMetadata.Keys.First();
            embeddingInputName = embeddingSession.InputMetadata.Keys.First();

            classifiers = BuildClassifierModelPaths(violaWakeSettings).ToDictionary(
                entry => entry.Key,
                entry => BuildClassifierSlot(logger, entry.Key, entry.Value));

            embeddingBufferMaxLength = classifiers.Values.Select(slot => slot.WindowFrames).DefaultIfEmpty(16).Max();
        }

        /// <summary>
        /// The wake word classifier is required and can't be auto-downloaded (it only exists once
        /// you've trained it on your own voice), so a blank/missing path fails loudly here with
        /// specific guidance - this is the single place that validates it, so nothing can silently
        /// construct a pipeline with no wake word classifier at all. Cancel-listening/abort-command
        /// are optional extras and are simply omitted if their file isn't present.
        /// </summary>
        private static IReadOnlyDictionary<string, string> BuildClassifierModelPaths(ViolaWakeSettings settings)
        {
            const string configFileRelativePath = "Config\\Nonconfidential\\WakeWord\\violawake-nonconfidential.json";
            const string trainClassifierNote = "record 10+ samples of yourself saying the wake word in the ViolaWake Console (https://violawake.com) and train a classifier";

            if (string.IsNullOrWhiteSpace(settings.WakeWordModelPath))
            {
                throw new InvalidOperationException($"ViolaWakeSettings.WakeWordModelPath is blank in {configFileRelativePath} - {trainClassifierNote}, then set this path to the .onnx file it gives you.");
            }

            if (!File.Exists(settings.WakeWordModelPath))
            {
                throw new InvalidOperationException($"No file exists at ViolaWakeSettings.WakeWordModelPath ('{settings.WakeWordModelPath}') - {trainClassifierNote}, then place the .onnx file it gives you at that path.");
            }

            Dictionary<string, string> classifierModelPaths = new()
            {
                [WakeWordClassifierName] = settings.WakeWordModelPath
            };

            //Cancel-listening/abort-command classifiers are optional extras - only wire them in if their trained model actually exists.
            if (!string.IsNullOrWhiteSpace(settings.CancelListeningModelPath) && File.Exists(settings.CancelListeningModelPath))
            {
                classifierModelPaths[CancelListeningClassifierName] = settings.CancelListeningModelPath;
            }

            if (!string.IsNullOrWhiteSpace(settings.AbortCommandModelPath) && File.Exists(settings.AbortCommandModelPath))
            {
                classifierModelPaths[AbortCommandClassifierName] = settings.AbortCommandModelPath;
            }

            return classifierModelPaths;
        }

        /// <summary>
        /// Loads an ONNX model and logs the exact path used, so a model missing from the output
        /// directory (e.g. forgotten CopyToOutputDirectory after adding a new file) shows up clearly
        /// in the log instead of surfacing only as an opaque OnnxRuntime exception.
        /// </summary>
        private static InferenceSession LoadModel(ILogger logger, string modelDescription, string modelPath)
        {
            string absolutePath = Path.GetFullPath(modelPath);

            if (!File.Exists(modelPath))
            {
                logger.LogError($"Could not load the {modelDescription} - no file exists at '{absolutePath}'. If you just added this file to the project, check that it's set to copy to the output directory.");
                throw new FileNotFoundException($"Missing {modelDescription}.", absolutePath);
            }

            logger.LogInformation($"Loading {modelDescription} from '{absolutePath}'...");
            InferenceSession session = new(modelPath);
            logger.LogInformation($"Loaded {modelDescription} from '{absolutePath}'.");

            return session;
        }

        private static ClassifierSlot BuildClassifierSlot(ILogger logger, string classifierName, string classifierModelPath)
        {
            InferenceSession session = LoadModel(logger, $"'{classifierName}' classifier model", classifierModelPath);
            KeyValuePair<string, NodeMetadata> input = session.InputMetadata.First();
            (int windowFrames, bool framesFirst) = InspectClassifierInputShape(input.Value.Dimensions);

            return new ClassifierSlot(session, input.Key, windowFrames, framesFirst);
        }

        /// <summary>
        /// A trained classifier's expected embedding-window depth and axis order aren't known
        /// until the model file exists, so they're read from the ONNX metadata instead of assumed.
        /// </summary>
        private static (int windowFrames, bool framesFirst) InspectClassifierInputShape(int[] dimensions)
        {
            if (dimensions.Length != 3)
            {
                return (16, true);
            }

            if (dimensions[2] == EmbeddingDim)
            {
                return (dimensions[1] > 0 ? dimensions[1] : 16, true);
            }

            if (dimensions[1] == EmbeddingDim)
            {
                return (dimensions[2] > 0 ? dimensions[2] : 16, false);
            }

            return (16, true);
        }

        /// <summary>
        /// Feeds newly captured 16kHz mono PCM samples through the pipeline. Returns the freshest
        /// confidence (0-1) per classifier name for classifiers that produced a new score this call;
        /// names with no fresh score (not enough audio yet) are omitted.
        /// </summary>
        public IReadOnlyDictionary<string, float> ProcessAudio(short[] newSamples)
        {
            pendingRawSamples.AddRange(newSamples);

            Dictionary<string, float> latestConfidences = new();

            while (pendingRawSamples.Count >= MelspecChunkSamples)
            {
                short[] chunk = pendingRawSamples.Take(MelspecChunkSamples).ToArray();
                pendingRawSamples.RemoveRange(0, MelspecChunkSamples);

                AppendMelFrames(chunk);
                chunksSinceLastTimingReport++;

                while (framesSinceLastEmbedding >= EmbeddingHopFrames && melFrameBuffer.Count >= EmbeddingWindowFrames)
                {
                    ComputeEmbedding();
                    embeddingsSinceLastTimingReport++;
                    framesSinceLastEmbedding -= EmbeddingHopFrames;

                    foreach (KeyValuePair<string, ClassifierSlot> entry in classifiers)
                    {
                        if (embeddingBuffer.Count >= entry.Value.WindowFrames)
                        {
                            //One call can drain more than one chunk, and so score more than once. Keeping the
                            //highest rather than the last matters because a detection peak is only one or two
                            //frames wide - overwriting would silently discard roughly a fifth of all scores,
                            //some of them the very frame a wake word was recognized on. Repeat firing is
                            //already handled by the caller's debounce, so a peak costs nothing to keep.
                            latestConfidences[entry.Key] = Math.Max(
                                latestConfidences.GetValueOrDefault(entry.Key),
                                RunClassifier(entry.Value));
                        }
                    }
                }

                ReportPipelineTimingIfDue();
            }

            return latestConfidences;
        }

        private void AppendMelFrames(short[] chunk)
        {
            //The melspectrogram model's analysis window (400 samples) is wider than its hop (160), so the
            //frames at the very start of a buffer need audio from before it. Running each 1280-sample chunk
            //in isolation therefore never produces the frames straddling the chunk boundary - measured at 5
            //frames per chunk instead of the 8 a continuous stream gives, leaving roughly 30ms of every 80ms
            //represented by no frame at all, and stretching the embedding time series the classifiers were
            //trained against. Replaying the trailing MelspecContextSamples restores them: this mirrors
            //openWakeWord's own streaming path, which feeds the model n_samples + 160*3 and keeps every
            //frame that comes back. Because MelspecChunkSamples is an exact multiple of the 160-sample hop,
            //the frame grid lands in the same place on every call, so the frames line up with a continuous
            //stream rather than merely approximating one.
            short[] samplesWithContext = melspecContext.Concat(chunk).ToArray();

            // openWakeWord casts int16 PCM straight to float32 for the melspectrogram model - no /32768 normalization.
            float[] floatSamples = Array.ConvertAll(samplesWithContext, sample => (float)sample);

            DenseTensor<float> inputTensor = new(floatSamples, new[] { 1, floatSamples.Length });
            List<NamedOnnxValue> inputs = new() { NamedOnnxValue.CreateFromTensor(melInputName, inputTensor) };

            using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = melspectrogramSession.Run(inputs);
            Tensor<float> melspecOutput = results.First().AsTensor<float>();
            // Actual runtime shape is (batch, 1, frames, 32) - confirmed against the model's own
            // output metadata, which reports Dimensions [-1, 1, -1, 32] (SymbolicDimensions "time",
            // "", "Clipoutput_dim_2", ""). The size-1 axis at index 1 is a leftover dummy dimension
            // from the model's original TF op and carries no data of its own.
            int frameCount = melspecOutput.Dimensions[2];

            //Every frame returned is new - the context only exists to let the model form the frames that
            //straddle the boundary, and it is short enough that it produces no frame of its own. The very
            //first call has no context yet and so comes up short by design; after that a short count means
            //the model's framing differs from what MelspecContextSamples assumes.
            bool isContextPrimed = melspecContext.Count == MelspecContextSamples;

            if (isContextPrimed && frameCount != MelFramesPerChunk && !hasWarnedAboutShortMelOutput)
            {
                hasWarnedAboutShortMelOutput = true;
                logger.LogWarning($"The melspectrogram model returned {frameCount} frames for {samplesWithContext.Length} samples, where {MelFramesPerChunk} were expected. The embedding time series will not match what the classifiers were trained on - MelspecContextSamples likely needs adjusting to this model's window and hop.");
            }

            for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                float[] frame = new float[MelBins];
                for (int melBinIndex = 0; melBinIndex < MelBins; melBinIndex++)
                {
                    // openWakeWord's transform to align with Google's native TF implementation.
                    frame[melBinIndex] = melspecOutput[0, 0, frameIndex, melBinIndex] / 10f + 2f;
                }

                melFrameBuffer.Add(frame);
            }

            melFramesPerChunkCounts[frameCount] = melFramesPerChunkCounts.GetValueOrDefault(frameCount) + 1;

            framesSinceLastEmbedding += frameCount;

            melspecContext.Clear();
            melspecContext.AddRange(samplesWithContext.Skip(Math.Max(0, samplesWithContext.Length - MelspecContextSamples)));

            int excessFrames = melFrameBuffer.Count - MelBufferMaxFrames;
            if (excessFrames > 0)
            {
                melFrameBuffer.RemoveRange(0, excessFrames);
            }
        }

        /// <summary>
        /// Reports how many mel frames the melspectrogram model actually produced per audio chunk, and how
        /// many embeddings that yielded. One embedding per chunk is the rate the classifier heads expect;
        /// anything less means the embedding time series is being stretched relative to training.
        /// </summary>
        private void ReportPipelineTimingIfDue()
        {
            if (chunksSinceLastTimingReport < ChunksBetweenTimingReports)
            {
                return;
            }

            if (logger.IsEnabled(LogLevel.Debug))
            {
                string framesPerChunkBreakdown = string.Join(", ", melFramesPerChunkCounts
                    .OrderBy(entry => entry.Key)
                    .Select(entry => $"{entry.Key} frames x{entry.Value}"));

                double embeddingsPerChunk = (double)embeddingsSinceLastTimingReport / chunksSinceLastTimingReport;

                logger.LogDebug($"Pipeline timing over {chunksSinceLastTimingReport} chunks of {MelspecChunkSamples} samples ({MelspecChunkMilliseconds}ms) each: mel frames per chunk = [{framesPerChunkBreakdown}]; embeddings produced = {embeddingsSinceLastTimingReport} ({embeddingsPerChunk:0.000} per chunk). Expect {EmbeddingHopFrames} frames per chunk and 1.000 embeddings per chunk - a lower rate means the embedding time series is stretched relative to what the classifiers were trained on.");
            }

            melFramesPerChunkCounts.Clear();
            chunksSinceLastTimingReport = 0;
            embeddingsSinceLastTimingReport = 0;
        }

        private void ComputeEmbedding()
        {
            float[] windowFrames = new float[EmbeddingWindowFrames * MelBins];
            int windowStart = melFrameBuffer.Count - EmbeddingWindowFrames;
            for (int frameIndex = 0; frameIndex < EmbeddingWindowFrames; frameIndex++)
            {
                Array.Copy(melFrameBuffer[windowStart + frameIndex], 0, windowFrames, frameIndex * MelBins, MelBins);
            }

            DenseTensor<float> inputTensor = new(windowFrames, new[] { 1, EmbeddingWindowFrames, MelBins, 1 });
            List<NamedOnnxValue> inputs = new() { NamedOnnxValue.CreateFromTensor(embeddingInputName, inputTensor) };

            using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = embeddingSession.Run(inputs);
            float[] embedding = results.First().AsTensor<float>().ToArray();

            embeddingBuffer.Add(embedding);

            int excessEmbeddings = embeddingBuffer.Count - embeddingBufferMaxLength;
            if (excessEmbeddings > 0)
            {
                embeddingBuffer.RemoveRange(0, excessEmbeddings);
            }
        }

        private float RunClassifier(ClassifierSlot slot)
        {
            float[][] window = embeddingBuffer.Skip(embeddingBuffer.Count - slot.WindowFrames).ToArray();

            float[] flattened = new float[slot.WindowFrames * EmbeddingDim];
            int[] shape;

            if (slot.FramesFirst)
            {
                for (int frameIndex = 0; frameIndex < slot.WindowFrames; frameIndex++)
                {
                    Array.Copy(window[frameIndex], 0, flattened, frameIndex * EmbeddingDim, EmbeddingDim);
                }

                shape = new[] { 1, slot.WindowFrames, EmbeddingDim };
            }
            else
            {
                for (int frameIndex = 0; frameIndex < slot.WindowFrames; frameIndex++)
                {
                    for (int embeddingDimIndex = 0; embeddingDimIndex < EmbeddingDim; embeddingDimIndex++)
                    {
                        flattened[(embeddingDimIndex * slot.WindowFrames) + frameIndex] = window[frameIndex][embeddingDimIndex];
                    }
                }

                shape = new[] { 1, EmbeddingDim, slot.WindowFrames };
            }

            DenseTensor<float> inputTensor = new(flattened, shape);
            List<NamedOnnxValue> inputs = new() { NamedOnnxValue.CreateFromTensor(slot.InputName, inputTensor) };

            using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = slot.Session.Run(inputs);
            float[] output = results.First().AsTensor<float>().ToArray();

            if (output.Length == 1)
            {
                return output[0];
            }

            float maxLogit = output.Max();
            float[] exponentials = output.Select(logit => (float)Math.Exp(logit - maxLogit)).ToArray();
            float sumOfExponentials = exponentials.Sum();
            return exponentials[^1] / sumOfExponentials;
        }

        public void Dispose()
        {
            melspectrogramSession.Dispose();
            embeddingSession.Dispose();

            foreach (ClassifierSlot slot in classifiers.Values)
            {
                slot.Session.Dispose();
            }
        }

        private sealed record ClassifierSlot(InferenceSession Session, string InputName, int WindowFrames, bool FramesFirst);
    }
}
