using System.Speech.Synthesis;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace StarmaidIntegrationComputer.SpeechSynthesis
{
    public class SpeechComputer
    {
        private readonly ILogger<SpeechComputer> logger;
        private readonly SpeechSynthesizer speechSynthesizer;

        private Regex removeCodeBlocksRegex = new Regex("```(.*)```", RegexOptions.Singleline);

        public Prompt? LastSpeech { get; private set; }

        public SpeechComputer(ILogger<SpeechComputer> logger)
        {
            this.logger = logger;

            speechSynthesizer = new SpeechSynthesizer();

            //TODO: To find installed voices if I continue to use SpeechSynthesizer, use:
            //  speechSynthesizer.GetInstalledVoices()
            speechSynthesizer.SelectVoice("Microsoft Zira Desktop");

        }
        public void Speak(string text)
        {
            logger.LogInformation($"Speaking: {text}");

            text = CleanUpScript(text);

            LastSpeech = speechSynthesizer.SpeakAsync(text);
        }

        public Task SpeakFakeAsync(string text)
        {
            Speak(text);
            return Task.CompletedTask;
        }

        private string CleanUpScript(string text)
        {
            text = text.Replace("Komette", "Comet");
            text = removeCodeBlocksRegex.Replace(text, "Sending you a code block.");

            return text;
        }
    }
}
