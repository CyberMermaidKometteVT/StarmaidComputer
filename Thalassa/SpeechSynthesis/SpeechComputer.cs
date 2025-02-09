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

        const string urlGroupName = "url";
        const string statusCodeGroupName = "statusCode";
        const string contentGroupName = "content";
        private Regex interpretOpenAiHttpError = new Regex(@"Error responding, error: Error at chat/completions (?<" + urlGroupName + @">\(.*\)) with HTTP status code: (?<" + statusCodeGroupName + @">\w+)\. Content: (?<" + contentGroupName + @">.*)$", RegexOptions.Singleline);

        public Prompt? LastSpeech { get; private set; }
        public bool IsSpeaking { get { return speechSynthesizer.State == SynthesizerState.Speaking; } }

        public List<Action> SpeechStartingHandlers { get; } = new List<Action>();
        public List<Action> SpeechCompletedHandlers { get; } = new List<Action>();

        private object locker = new object();

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


            //Add a toggle that will switch between speech systems
#warning 
            if (thalassaSettings.UseOpenAiTts)
            {


                ThreadPool.QueueUserWorkItem(_ =>
                {

                    OpenAIClient aiClient = new OpenAIClient(openAISensitiveSettings.OpenAIBearerToken);
                    OpenAI.Audio.AudioClient audioClient = aiClient.GetAudioClient("tts-1");
                    var speechResult = audioClient.GenerateSpeech(text, GeneratedSpeechVoice.Nova);


                    using var memoryStream = new MemoryStream(speechResult.Value.ToArray());
                    using var mp3FileReader = new Mp3FileReader(memoryStream);
                    using var waveOut = new WaveOutEvent();
                    waveOut.Init(mp3FileReader);
                    waveOut.Volume = 1f;

                    lock (locker)
                    {
                        waveOut.Play();

                        while (waveOut.PlaybackState == PlaybackState.Playing)
                        {
                            Thread.Sleep(100);
                        }
                    }
                });

            }
            else
            {
                LastSpeech = speechSynthesizer.SpeakAsync(text);
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
            var parsedJsonError = JsonSerializer.Deserialize<ParsedOpenAiError>(errorContent)?.error;
            //url, statusCode, content

            string statusCodeMessage = errorStatusCode != null ? $"{errorStatusCode}, " : "";

            text = $"OpenAI error. {statusCodeMessage}. {parsedJsonError.code}. See log for more details.";

            return text.Replace('_', ' ');
        }

    }
}
