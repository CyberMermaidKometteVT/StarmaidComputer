using System.Speech.Recognition;

using Microsoft.Extensions.Logging;

using StarmaidIntegrationComputer.Common.DataStructures;
using StarmaidIntegrationComputer.Common.Settings.Interfaces;
using StarmaidIntegrationComputer.Common.TasksAndExecution;
using StarmaidIntegrationComputer.Thalassa.VoiceToText;

namespace StarmaidIntegrationComputer.Thalassa
{
    public class ThalassaCore : IDisposable
    {
        public const string WAKE_WORD = "Thalassa";
        private readonly VoiceToTextManager voiceToTextManager;
        private readonly IThalassaCoreSettings settings;
        private readonly StarmaidStateBag stateBag;
        SpeechRecognitionEngine recognitionEngine = new SpeechRecognitionEngine();

        public bool Listening { get; private set; }
        public Action<string>? DisplayInput { get; set; }
        public Action<string>? SpeechInterpreted { get; set; }

        private ILogger<ThalassaCore> Logger { get; set; }

        private ILoggerFactory loggerFactory;
        public ILoggerFactory LoggerFactory
        {
            get { return loggerFactory; }
            set
            {
                loggerFactory = value;
                Logger = loggerFactory?.CreateLogger<ThalassaCore>();
            }
        }

        public List<Action> StartingListeningHandlers { get; } = new List<Action>();
        public List<Action> StoppingListeningHandlers { get; } = new List<Action>();

        public ThalassaCore(VoiceToTextManager voiceToTextManager, IThalassaCoreSettings settings, StarmaidStateBag stateBag)
        {
            this.voiceToTextManager = voiceToTextManager;
            this.settings = settings;
            this.stateBag = stateBag;
            //var builder = new GrammarBuilder(WAKE_WORD);
            //builder.Append("Alexa");

            //Find sound-alike words at https://www.rhymezone.com/r/rhyme.cgi?org1=syl&org2=l&typeofrhyme=sim&Word=Thalassa
            List<string> wakeWordAndSoundalikes = new List<string>{ WAKE_WORD, "thalasso", "thalassic", "Kailasa", "Colossae", "Kolasa", "Salassi", "Felasa", "thawless", "the last", "the least", "the list", "the lesson", "thallus", "thalami", "thallous", "Thalassoma", "tholus", "balasa", "thelazia", "Thalys", "thalassian", "Melissa", "Khalsa", "Tolosa", "halacha", "colossi", "Colusa", "Malissa", "Calusa", "Felisa", "malacia", "Melisa", "calesa", "Filosa", "dalasi", "jalsa", "Mellisa", "malisa", "Mellissa", "phalsa", "julissa", "Aglossa", "Milissa", "Folusa", "tulisa", "melosa", "calsa", "thalluses", "the lot", "the lodge", "Thelma", "salsa", "Thalia", "tholos", "falsa", "Galasso", "tholeiite", "thylacine", "Alosa", "tholoi", "theileria", "thelema", "Tholen", "Thraso", "Yalsa", "Alausa", "Thielavia", "thulia", "the holy see", "policy", "fallacy", "class a", "colossal", "solace", "holy see", "Elisa", "Hollis", "Silesia", "Tulsa", "colossus", "salas", "Selassie", "mollusca", "Collis", "yellow sea", "Alyssa", "mollusc", "philosophe", "Walesa", "mollusk", "salama", "Manasa", "Alisa", "Malia", "salles", "Mollison", "Collison", "Alissa", "salis", "mollis", "Alexa" };
            Choices choices = new Choices(wakeWordAndSoundalikes.ToArray());
            var builder = choices.ToGrammarBuilder();

            recognitionEngine.LoadGrammar(new Grammar(builder));
            recognitionEngine.SpeechRecognized += Recognizer_SpeechRecognized;
            recognitionEngine.SpeechRecognitionRejected += RecognitionEngine_SpeechRecognitionRejected;
            recognitionEngine.SetInputToDefaultAudioDevice();
            recognitionEngine.EndSilenceTimeout = TimeSpan.FromMilliseconds(50);
        }

        public void Dispose()
        {
            recognitionEngine?.Dispose();
            recognitionEngine = null;
        }

        public void StartListening()
        {
            if (recognitionEngine == null)
            {
                throw new InvalidOperationException($"Error - trying to use a disposed {GetType().Name}!  Get a new one!");
            }

            recognitionEngine.RecognizeAsync(RecognizeMode.Multiple);
            Listening = true;

            Logger.LogInformation("Thalassa AI is listening...");
        }

        public void StopListening()
        {
            if (recognitionEngine == null)
            {
                return;
            }

            recognitionEngine.RecognizeAsyncStop();
            Listening = false;

            Logger.LogInformation("Thalassa AI is sleeping...");
        }

        private void Recognizer_SpeechRecognized(object? sender, SpeechRecognizedEventArgs e)
        {
            string textToDisplay = $"({e.Result.Confidence}): {e.Result.Text}";
            Logger.LogInformation($"Thalassa Speech Recognized: {textToDisplay}");

            DisplayIfAble(textToDisplay);

            if (e.Result.Confidence > settings.WakeWordConfidenceThreshold && e.Result.Text.Contains(WAKE_WORD))
            {
                Logger.LogInformation($"Wake word identified!  Starting to listen to what comes next!");

                string raidersAsString = string.Join(", ", stateBag.Raiders.Select(raider => raider.RaiderName));
                string chattersAsString = string.Join(", ", stateBag.Chatters.Select(chatter => chatter.ChatterName));
                string viewersAsString = string.Join(", ", stateBag.Viewers);
                string context = $"This transcript is interpreting the spoken communication between the cyber mermaid Komette, who is a VTuber, talking to her shipboard AI, Thalassa. Komette is a character that streams on Twitch. It is important that the transcription correctly identify usernames, which might have weird capitalization, numbers, and special characters.  Those special characters might sound like the word describing those character like \"ampersand\" for &, they might sound like other letters they're meant to represent like \"L\" for \"1\", or might be entirely silent, like an \"_\"!  Also, consider raiders before chatters, and chatters before viewrs.\r\nRecent raiders: {raidersAsString}\r\nRecent chatters: {chattersAsString}\r\nCurrent viewers: {viewersAsString}\r\n\r\nHere are some examples, which will be in the format: \"PROMPT: (prompt) | INPUT: (speech that's been interpreted to text) | OUTPUT: (the expected response)\r\nPROMPT: Current viewers: CyberMermaidKomette_VT | INPUT: Cyber Mermaid Komette VT | OUTPUT: CyberMermaidKomette_VT\r\nPROMPT: Recent chatters: damien_verde_ch | INPUT: Damien Verde C H | damien_verde_ch";


                Logger.LogInformation($"Passing in to our voice to text manager the following context: \r\n{context}");

                var result = voiceToTextManager.StartListeningAndInterpret(context).ContinueWith(ReactToSpeech);
                StartingListeningHandlers.Invoke();
            }
        }

        private void ReactToSpeech(Task<string> completeInterpretationTask)
        {
            StoppingListeningHandlers.Invoke();

            //Bad case! :(
            if (completeInterpretationTask.Exception != null)
            {
                string errorMessage = $"Error interpreting speech! Error message: {completeInterpretationTask.Exception.Message}";
                Logger.LogError(completeInterpretationTask.Exception, errorMessage);
                DisplayIfAble(errorMessage);
                return;
            }

            Logger.LogInformation($"Speech after the wake word: {completeInterpretationTask.Result}");

            //Bypassing case!  ^.^,
            if (completeInterpretationTask.Result == VoiceToTextManager.ALREADY_LISTENING_RESULT)
            {
                Logger.LogInformation($"Skipping interpreting the speech, as we were already interpreting it in a different task!`");
                return;
            }

            //Good case! :D
            DisplayIfAble($"OpenAI heard: {completeInterpretationTask.Result}");
            SpeechInterpreted(completeInterpretationTask.Result);
        }

        private void RecognitionEngine_SpeechRecognitionRejected(object? sender, SpeechRecognitionRejectedEventArgs e)
        {

            string textToDisplay = $"({e.Result.Confidence}): {e.Result.Text}";
            Logger.LogInformation($"Thalassa REJECTED Speech: {textToDisplay}");

            DisplayIfAble(textToDisplay);
        }

        private void DisplayIfAble(string textToDisplay)
        {
            if (DisplayInput != null)
            {
                DisplayInput(textToDisplay);
                //Consider throwing an error if DisplayInput is unset
            }
        }

    }
}