﻿using System.Speech.Recognition;

using Microsoft.Extensions.Logging;

namespace Thalassa
{
    public class ThalassaCore : IDisposable
    {
        public const string WAKE_WORD = "Thalassa";


        SpeechRecognitionEngine recognitionEngine = new SpeechRecognitionEngine();

        public bool Listening { get; private set; }
        public Action<string>? DisplayInput { get; set; }

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

        public ThalassaCore()
        {
            recognitionEngine.LoadGrammar(new Grammar(new GrammarBuilder("Thalassa")));
            recognitionEngine.SpeechRecognized += Recognizer_SpeechRecognized;
            recognitionEngine.SpeechRecognitionRejected += RecognitionEngine_SpeechRecognitionRejected;
            recognitionEngine.SetInputToDefaultAudioDevice();

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
            if (DisplayInput != null)
            {

                if (DisplayInput != null)
                {
                    DisplayInput(textToDisplay);
                    //Consider throwing an error if DisplayInput is unset
                }
            }
        }


        private void RecognitionEngine_SpeechRecognitionRejected(object? sender, SpeechRecognitionRejectedEventArgs e)
        {

            string textToDisplay = $"({e.Result.Confidence}): {e.Result.Text}";
            Logger.LogInformation($"Thalassa REJECTED Speech: {textToDisplay}");
            if (DisplayInput != null)
            {

                if (DisplayInput != null)
                {
                    DisplayInput(textToDisplay);
                    //Consider throwing an error if DisplayInput is unset
                }
            }
        }

    }
}