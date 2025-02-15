using System.Speech.Synthesis;
using System.Text.Json;
using System.Text.RegularExpressions;

using Microsoft.Extensions.Logging;

using OpenAI.Audio;
using OpenAI;

using StarmaidIntegrationComputer.Common.TasksAndExecution;
using StarmaidIntegrationComputer.Thalassa.OpenAiCommon.JsonParsing;
using StarmaidIntegrationComputer.Thalassa.Settings;
using NAudio.Wave;

namespace StarmaidIntegrationComputer.Thalassa.SpeechSynthesis
{
    public class SpeechComputer
    {
        private readonly ILogger<SpeechComputer> logger;
        private readonly ThalassaSettings thalassaSettings;
        private readonly OpenAISensitiveSettings openAISensitiveSettings;
        private readonly SpeechSynthesizer speechSynthesizer;
        private readonly SpeechReplacements speechReplacements;

        private Regex removeCodeBlocksRegex = new Regex("```(.*)```", RegexOptions.Singleline);

        private const string urlGroupName = "url";
        private const string statusCodeGroupName = "statusCode";
        private const string contentGroupName = "content";
        private Regex interpretOpenAiHttpError = new Regex(@"Error responding, error: Error at chat/completions (?<" + urlGroupName + @">\(.*\)) with HTTP status code: (?<" + statusCodeGroupName + @">\w+)\. Content: (?<" + contentGroupName + @">.*)$", RegexOptions.Singleline);
        private CancellationTokenSource? cancellationTokenSource = null;
        private object openAiConsideringSpeaking = new object();
        private object openAiSpeaking = new object();
        private int runningOpenAiSpeechThreads = 0;
        private Thread currentThread = Thread.CurrentThread;

        public bool IsSpeaking { get { return speechSynthesizer.State == SynthesizerState.Speaking || runningOpenAiSpeechThreads != 0; } }

        public List<Action> SpeechStartingHandlers { get; } = new List<Action>();
        public List<Action> SpeechCompletedHandlers { get; } = new List<Action>();


        public SpeechComputer(ILogger<SpeechComputer> logger, ThalassaSettings thalassaSettings, OpenAISensitiveSettings openAISensitiveSettings, SpeechReplacements speechReplacements)
        {
            this.logger = logger;
            this.thalassaSettings = thalassaSettings;
            this.openAISensitiveSettings = openAISensitiveSettings;
            this.speechReplacements = speechReplacements;

            speechSynthesizer = new SpeechSynthesizer();

            //TODO: To find installed voices if I continue to use SpeechSynthesizer, use:
            //  speechSynthesizer.GetInstalledVoices()
            speechSynthesizer.SelectVoice("Microsoft Zira Desktop");

            speechSynthesizer.SpeakStarted += SpeechSynthesizer_SpeakStarted;
            speechSynthesizer.SpeakCompleted += SpeechSynthesizer_SpeakCompleted;

        }

        private void SpeechSynthesizer_SpeakCompleted(object? sender, SpeakCompletedEventArgs e)
        {
            SpeechCompletedHandlers.Invoke();
        }

        private void SpeechSynthesizer_SpeakStarted(object? sender, SpeakStartedEventArgs e)
        {
            SpeechStartingHandlers.Invoke();
        }

        public void Speak(string text)
        {
            logger.LogInformation($"Speaking: {text}");

            text = CleanUpScript(text);

            int workerthreads;
            int completionPortThreads;
            ThreadPool.GetMaxThreads(out workerthreads, out completionPortThreads);

            logger.LogInformation($"Max threads: worker threads {workerthreads}, completion port threads {completionPortThreads}; currently running: {ThreadPool.ThreadCount}");

            if (thalassaSettings.UseOpenAiTts)
            {
                lock (openAiConsideringSpeaking)
                {
                    if (runningOpenAiSpeechThreads == 0)
                    {
                        cancellationTokenSource = new CancellationTokenSource();
                    }

                    if (!this.IsSpeaking)
                    {
                        SpeechStartingHandlers.Invoke();
                    }

                    runningOpenAiSpeechThreads++;
                }


                ThreadPool.QueueUserWorkItem(_ =>
                        {
                            try
                            {
                                if (cancellationTokenSource.Token.IsCancellationRequested)
                                {
                                    logger.LogInformation("Speech abort requested; aborting speech from OpenAI TTS.");
                                    return;
                                }

                                logger.LogInformation("Sending speech to OpenAI TTS.");


                                OpenAIClient aiClient = new OpenAIClient(openAISensitiveSettings.OpenAIBearerToken);
                                AudioClient audioClient = aiClient.GetAudioClient("tts-1");
                                System.ClientModel.ClientResult<BinaryData> speechResult = audioClient.GenerateSpeech(text, GeneratedSpeechVoice.Nova);
                                logger.LogInformation("Speech mp3 data received from OpenAI TTS. Playing now.");


                                if (cancellationTokenSource.Token.IsCancellationRequested)
                                {
                                    logger.LogInformation("Speech abort requested; aborting speech from OpenAI TTS.");
                                    return;
                                }

                                using MemoryStream memoryStream = new MemoryStream(speechResult.Value.ToArray());
                                using Mp3FileReader mp3FileReader = new Mp3FileReader(memoryStream);
                                using WaveOutEvent waveOut = new WaveOutEvent();
                                waveOut.Init(mp3FileReader);
                                waveOut.Volume = 1f;

                                lock (openAiSpeaking)
                                {
                                    if (cancellationTokenSource.Token.IsCancellationRequested)
                                    {
                                        logger.LogInformation("Speech abort requested; aborting speech from OpenAI TTS.");
                                        return;
                                    }

                                    waveOut.Play();

                                    while (waveOut.PlaybackState == PlaybackState.Playing)
                                    {
                                        if (cancellationTokenSource.Token.IsCancellationRequested)
                                        {
                                            logger.LogInformation("Speech abort requested; aborting speech from OpenAI TTS.");
                                            return;
                                        }

                                        Thread.Sleep(100);
                                    }
                                }
                            }
                            catch (Exception ex) when (ex is not ThreadAbortException)
                            {
                                logger.LogError($"Failed to speak - {ex.ToString()}");
                            }
                            finally
                            {
                                runningOpenAiSpeechThreads--;
                                if (!this.IsSpeaking)
                                {
                                    SpeechCompletedHandlers.Invoke();
                                }
                            }
                        });
            }
            else
            {
                speechSynthesizer.SpeakAsync(text);
            }

        }

        public Task SpeakFakeAsync(string text)
        {
            Speak(text);
            return Task.CompletedTask;
        }

        public void CancelSpeech()
        {
            speechSynthesizer.SpeakAsyncCancelAll();
            cancellationTokenSource?.Cancel();
        }

        private string CleanUpScript(string text)
        {
            text = CleanUpCodeBlocks(text);

            text = CleanUpThalassaErrors(text);

            return text;
        }

        private string CleanUpCodeBlocks(string text)
        {
            text = removeCodeBlocksRegex.Replace(text, "Sending you a code block.");

            foreach (SpeechReplacement speechReplacement in speechReplacements.Replacements)
            {
                text = text.Replace(speechReplacement.Phrase, speechReplacement.Replacement, StringComparison.CurrentCultureIgnoreCase);
            }

            return text;
        }
        private string CleanUpThalassaErrors(string text)
        {
            Match? errorMatch = interpretOpenAiHttpError.Match(text);

            if (errorMatch == null || errorMatch.Value.Length == 0)
            {
                return text;
            }

            Group? contentGroup = null;
            Group? statusCodeGroup = null;

            bool errorContentFound = errorMatch?.Groups.TryGetValue(contentGroupName, out contentGroup) ?? false;

            if (!errorContentFound)
            {
                return $"Couldn't understand this error: {text}";
            }

            bool errorStatusCodeFound = errorMatch?.Groups.TryGetValue(statusCodeGroupName, out statusCodeGroup) ?? false;

            string errorContent = contentGroup.Value;
            string? errorStatusCode = statusCodeGroup?.Value;
            ParsedOpenAiError.ParsedError? parsedJsonError = JsonSerializer.Deserialize<ParsedOpenAiError>(errorContent)?.error;
            //url, statusCode, content

            string statusCodeMessage = errorStatusCode != null ? $"{errorStatusCode}, " : "";

            text = $"OpenAI error. {statusCodeMessage}. {parsedJsonError.code}. See log for more details.";

            return text.Replace('_', ' ');
        }

    }
}
