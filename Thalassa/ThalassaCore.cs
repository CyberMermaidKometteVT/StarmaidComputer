﻿using System.Speech.Recognition;

using Microsoft.Extensions.Logging;

using StarmaidIntegrationComputer.Common.Settings.Interfaces;
using StarmaidIntegrationComputer.Common.TasksAndExecution;
using StarmaidIntegrationComputer.Thalassa.VoiceToText;

namespace StarmaidIntegrationComputer.Thalassa
{
    public class ThalassaCore : IDisposable
    {
        public const string WAKE_WORD = "Thalassa";
        public const string ABORT_PHRASE = WAKE_WORD + " Abort";
        public const float ABORT_THRESHOLD = 0.80f;
        private readonly VoiceToTextManager voiceToTextManager;
        private readonly IThalassaCoreSettings settings;
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

        public ThalassaCore(VoiceToTextManager voiceToTextManager, IThalassaCoreSettings settings)
        {
            this.voiceToTextManager = voiceToTextManager;
            this.settings = settings;

            var wakeWordBuilder = new GrammarBuilder(WAKE_WORD);
            var alexaToIgnoreBuilder = new GrammarBuilder("Alexa");

            List<GrammarBuilder> rootGrammars = new List<GrammarBuilder> { wakeWordBuilder };
            if (WAKE_WORD != "Alexa")
            {
                rootGrammars.Add(alexaToIgnoreBuilder);
            }

            var interruptBuilder = new GrammarBuilder(ABORT_PHRASE);
            rootGrammars.Add(interruptBuilder);


            var rootGrammarBuilder = new Choices(rootGrammars.ToArray());

            recognitionEngine.LoadGrammar(new Grammar(rootGrammarBuilder));
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

            if (e.Result.Confidence > settings.WakeWordConfidenceThreshold && e.Result.Text == WAKE_WORD)
            {
                Logger.LogInformation($"Wake word identified!  Starting to listen to what comes next!");
                var result = voiceToTextManager.StartListeningAndInterpret().ContinueWith(ReactToSpeech);
                StartingListeningHandlers.Invoke();
            }

            if (e.Result.Confidence > ABORT_THRESHOLD && e.Result.Text == ABORT_PHRASE)
            {
                Logger.LogInformation($"Abort phrase identified! Interrupting action!");

                voiceToTextManager.AbortCurrentListening();
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

            //Bypassing case: Abort issued
            if (completeInterpretationTask.Status == TaskStatus.Canceled)
            {

                string abortMessage = $"ABORT ISSUED, NOT INTERPRETING SPEECH";
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