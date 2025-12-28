using System.Speech.Recognition;

using Microsoft.Extensions.Logging;

using StarmaidIntegrationComputer.Common.DataStructures.StarmaidState;
using StarmaidIntegrationComputer.Common.Settings;
using StarmaidIntegrationComputer.Common.TasksAndExecution;
using StarmaidIntegrationComputer.Thalassa.Settings;
using StarmaidIntegrationComputer.Thalassa.SpeechSynthesis;
using StarmaidIntegrationComputer.Thalassa.VoiceToText;
using StarmaidIntegrationComputer.Thalassa.WakeWordProcessor;

namespace StarmaidIntegrationComputer.Thalassa
{
    public class ThalassaCore : IDisposable
    {
        private readonly VoiceToTextManager voiceToTextManager;
        private readonly ThalassaSettings settings;
        private readonly AudienceRegistry audienceRegistry;
        private readonly StreamerProfileSettings streamerProfileSettings;
        private readonly SpeechComputer speechComputer;

        private readonly WakeWordProcessorBase wakeWordProcessor;

        private bool isListening = false;

        public bool IsListening
        {
            get { return isListening; }
            set
            {
                isListening = value;
                IsListeningChangedHandlers.Invoke();
            }
        }

        public Action<string>? DisplayInput { get; set; }
        public Action<string>? SpeechInterpreted { get; set; }
        public Action? AbortCommandIssued { get; set; }

        private ILogger<ThalassaCore>? Logger { get; set; }

        private ILoggerFactory? loggerFactory;
        public ILoggerFactory? LoggerFactory
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
        public List<Action> IsListeningChangedHandlers { get; } = new List<Action>();

        public ThalassaCore(VoiceToTextManager voiceToTextManager, ThalassaSettings settings, AudienceRegistry audienceRegistry, IWakeWordProcessorFactory wakeWordProcessorFactory, StreamerProfileSettings streamerProfileSettings, SpeechComputer speechComputer)
        {
            this.voiceToTextManager = voiceToTextManager;
            this.settings = settings;
            this.audienceRegistry = audienceRegistry;
            this.wakeWordProcessor = wakeWordProcessorFactory.Processor;
            this.streamerProfileSettings = streamerProfileSettings;
            this.speechComputer = speechComputer;

            wakeWordProcessor.OnWakeWordHeard = WakeWordHeard;
            wakeWordProcessor.OnCancelListeningHeard = CancelCurrentListening;
            wakeWordProcessor.OnAbortCommandHeard = AbortCommandHeard;

            wakeWordProcessor.DisplayIfAble = DisplayIfAble;
        }

        private void WakeWordHeard()
        {
            Logger.LogInformation("Listening for speech...");

            string context = GetContextForVoiceInterpretation();
            voiceToTextManager.StartListeningAndInterpret(context).ContinueWith(ReactToSpeech);
            StartingListeningHandlers.Invoke();
        }

        private void AbortCommandHeard()
        {

            if (AbortCommandIssued != null)
            {
                Logger.LogInformation($"Abort command phrase identified! Aborting command!");
                AbortCommandIssued();
            }
            else
            {
                Logger.LogInformation($"Abort command phrase identified, but its behavior has not been wired up!");
            }

            CancelCurrentListening();
        }

        public void Dispose()
        {
            wakeWordProcessor.Dispose();
        }

        public void StartListening()
        {
            if (IsListening)
            {
                return;
            }

            IsListening = true;
            wakeWordProcessor.StartListening();

            Logger.LogInformation($"{streamerProfileSettings.AiName} is listening...");
        }

        public void StopListening()
        {
            if (!IsListening)
            {
                return;
            }

            IsListening = false;
            wakeWordProcessor.StopListening();

            Logger.LogInformation($"{streamerProfileSettings.AiName} is sleeping...");
        }

        public void CancelCurrentListening()
        {
            voiceToTextManager.CancelCurrentListening();
        }

        public void ConcludeCurrentListening()
        {
            voiceToTextManager.ConcludeCurrentListening();
        }

        public void CancelSpeech()
        {
            speechComputer.CancelSpeech();
        }

        private string GetContextForVoiceInterpretation()
        {
            string raidersAsString = string.Join(", ", audienceRegistry.Raiders.Select(raider => raider.RaiderName));
            string chattersAsString = string.Join(", ", audienceRegistry.Chatters.Select(chatter => chatter.ChatterName));
            string viewersAsString = string.Join(", ", audienceRegistry.Viewers);
            string context = $"This transcript is interpreting the spoken communication between {streamerProfileSettings.StreamerName}, {streamerProfileSettings.StreamerDescription}. They will be talking to {streamerProfileSettings.AiName}, their {streamerProfileSettings.AiDescription}. {streamerProfileSettings.StreamerName} is {streamerProfileSettings.StreamerMetaDescription}. It is important that the transcription correctly identify usernames, which might have weird capitalization, numbers, and special characters.  Those special characters might sound like the word describing those character like \"ampersand\" for &, they might sound like other letters they're meant to represent like \"L\" for \"1\", or might be entirely silent, like an \"_\"!  Also, consider raiders before chatters, and chatters before viewers.\r\nRecent raiders: {raidersAsString}\r\nRecent chatters: {chattersAsString}\r\nCurrent viewers: {viewersAsString}\r\n\r\nHere are some examples, which will be in the format: \"PROMPT: (prompt) | INPUT: (speech that's been interpreted to text) | OUTPUT: (the expected response)\r\nPROMPT: Current viewers: CyberMermaidKomette_VT | INPUT: Cyber Mermaid Komette VT | OUTPUT: CyberMermaidKomette_VT\r\nPROMPT: Recent chatters: damien_verde_ch | INPUT: Damien Verde C H | damien_verde_ch";
            return context;
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

            //Bypassing case: Abort issued
            if (completeInterpretationTask.Status == TaskStatus.Canceled)
            {
                string abortMessage = $"CANCEL ISSUED, NOT INTERPRETING SPEECH";
                Logger.LogInformation(abortMessage);
                DisplayIfAble(abortMessage);
                return;
            }

            Logger.LogInformation($"Speech after the wake word: {completeInterpretationTask.Result}");

            //Bypassing case: Already listening, not requeuing!  ^.^,
            if (completeInterpretationTask.Result == VoiceToTextManager.ALREADY_LISTENING_RESULT)
            {
                Logger.LogInformation($"Skipping interpreting the speech, as we were already interpreting it in a different task!`");
                return;
            }

            //Good case! :D
            DisplayIfAble($"OpenAI heard: {completeInterpretationTask.Result}");
            SpeechInterpreted(completeInterpretationTask.Result);
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