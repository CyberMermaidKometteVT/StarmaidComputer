using System.Collections.Concurrent;

using Microsoft.Extensions.Logging;

using OpenAI.Audio;

using OpenAI;

using StarmaidIntegrationComputer.Common.TasksAndExecution;
using StarmaidIntegrationComputer.Thalassa.Settings;
using NAudio.Wave;

namespace StarmaidIntegrationComputer.Thalassa.SpeechSynthesis
{

    //TODO: Should I genericize this dispatcher? Some kind of MultiProcessSingleOutput thing? That's ... maybe not the name, I dunno...
    // Either way I should probably move the ProcessTextForSpeech() and OutputSpeech() methods to another class...but at time of development
    // I am too tired to be arsed with moving the aborting behavior to another class. 😡
    public class OpenAiTtsDispatcher : IOpenAiTtsDispatcher
    {
        public bool IsSpeaking { get; private set; }

        public Action DoneSpeaking { get; set; } = () => { };

        private readonly ConcurrentQueue<CancellableWorkerTask<byte[]>> processingTasks = new ConcurrentQueue<CancellableWorkerTask<byte[]>>();

        private readonly ILogger<OpenAiTtsDispatcher> logger;
        private readonly OpenAISensitiveSettings openAISensitiveSettings;
        private bool aborting = false;
        private object isSpeakingLocker = new object();

        private readonly ManualResetEventSlim taskAvailable = new ManualResetEventSlim();


        public OpenAiTtsDispatcher(ILogger<OpenAiTtsDispatcher> logger, OpenAISensitiveSettings openAISensitiveSettings)
        {
            Task.Run(StartListeningForSpeechEvents);
            this.logger = logger;
            this.openAISensitiveSettings = openAISensitiveSettings;
        }

        public void Speak(string textToSpeak)
        {
            string textToSpeakSummary = SummarizeTextToSpeak(textToSpeak);
            logger.LogInformation($"Speak call received for \"{textToSpeakSummary}\" This will be enqueued soon.");
            lock (isSpeakingLocker)
            {
                if (aborting) return;
#pragma warning disable CS8604 // Possible null reference argument.
                CancellableWorkerTask<byte[]>? cancellableTask = null;
                cancellableTask = new CancellableWorkerTask<byte[]>(() => ProcessTextForSpeech(textToSpeak, cancellableTask));
#pragma warning restore CS8604 // Possible null reference argument.

                logger.LogInformation("Komette thinks this might mysteriously not be getting hit when Thalassa is unexpectedly not talking; let's see if it is!");
                //Lock on ProcessingTasks?
                processingTasks.Enqueue(cancellableTask);
                logger.LogInformation($"Speak call for \"{textToSpeakSummary}\" enqueued.");
                IsSpeaking = true;
            }
        }

        public void Abort()
        {
            logger.LogInformation("Aborting TTS.");
            //Locking processingTasks?
            aborting = true;
            lock (isSpeakingLocker)
            {
                logger.LogInformation("Aborting TTS - in locker.");
                int abortedTaskCount = 0;
                foreach (CancellableWorkerTask<byte[]> cancelableTask in processingTasks)
                {
                    cancelableTask.CancellationTokenSource.Cancel();
                    abortedTaskCount++;
                }

                Task[] abortingTasks = processingTasks
                    .Select(cancellableWorker => cancellableWorker.Task)
                    .ToArray();
                processingTasks.Clear();

                if (abortedTaskCount > 0)
                {
                    logger.LogInformation($"{abortedTaskCount} speak calls dequeued due to abort.");
                    DoneSpeaking();

                    logger.LogInformation($"Waiting for all tasks to exit...");

                    Task.WhenAll(abortingTasks)
                        .ContinueWith(task =>
                        {
                            logger.LogInformation("All speaking tasks have exited, clearing aborting flag ...");
                            IsSpeaking = false;
                            aborting = false;

                            taskAvailable.Set();
                        });
                }
                else
                {
                    taskAvailable.Set();
                }
            }
        }

        private static string SummarizeTextToSpeak(string textToSpeak)
        {
            int textToSpeakSummaryLength = textToSpeak.Length > 10 ? 10 : textToSpeak.Length;
            string textToSpeakSummary = textToSpeak.Substring(0, textToSpeakSummaryLength) + (textToSpeakSummaryLength >= 10 ? "..." : "");
            return textToSpeakSummary;
        }

        private async Task StartListeningForSpeechEvents()
        {
            //Infinite loop to listen for the lifetime of the application
            while (true)
            {
                //TODO: OpenAI seems to think that this is more performant than a Task.Delay()? I'm pretty sure it's not, we're gonna be looping, a lot!

                taskAvailable.Wait();

                if (aborting) continue;

                bool peekSuccessful = processingTasks.TryPeek(out CancellableWorkerTask<byte[]>? firstTask);
                if (peekSuccessful && firstTask.IsWorkComplete && !aborting)
                {
                    logger.LogInformation($"Speak call starting to speak. Presently there are {processingTasks.Count} speech processing tasks in the queue.");
                    IsSpeaking = true;

                    byte outputSpeechAttempts = 0;
                    for (; outputSpeechAttempts < 3; outputSpeechAttempts++)
                    {
                        try
                        {

                            await OutputSpeech(firstTask.WorkOutput, firstTask.CancellationTokenSource);
                            break;
                        }
                        catch (Exception ex)
                        {
                            logger.LogError($"Failed to output speech {outputSpeechAttempts + 1} times.");
                        }
                    }

                    if (outputSpeechAttempts == 3)
                    {
                        logger.LogError("Gave up on trying to output speech. Moving on.");
                    }

                    logger.LogInformation($"Dequeueing task while there are {processingTasks.Count} speech processing tasks in the queue.");

                    processingTasks.TryDequeue(out firstTask);

                    lock (isSpeakingLocker)
                    {
                        if (processingTasks.IsEmpty)
                        {
                            DoneSpeaking();
                        }
                    }
                }

                lock (isSpeakingLocker)
                {
                    if (processingTasks.IsEmpty)
                    {
                        IsSpeaking = false;
                        aborting = false;
                        taskAvailable.Reset();
                    }
                }

            }
        }


        private void ProcessTextForSpeech(string textToSpeak, CancellableWorkerTask<byte[]> cancellableTask)
        {
            logger.LogInformation($"Speak call beginning to process: {SummarizeTextToSpeak(textToSpeak)}");

            if (cancellableTask.CancellationTokenSource.Token.IsCancellationRequested)
            {
                logger.LogInformation("Speech abort requested; aborting speech from OpenAI TTS.");
                return;
            }

            logger.LogInformation("Sending speech to OpenAI TTS.");


            OpenAIClient aiClient = new OpenAIClient(openAISensitiveSettings.OpenAIBearerToken);
            AudioClient audioClient = aiClient.GetAudioClient("tts-1");
            System.ClientModel.ClientResult<BinaryData> speechResult = audioClient.GenerateSpeech(textToSpeak, GeneratedSpeechVoice.Nova);
            byte[] returnValue = speechResult.Value.ToArray();
            cancellableTask.WorkOutput = returnValue;
            logger.LogInformation("Speech mp3 data received from OpenAI TTS. Will play when playing is available..");


            if (cancellableTask.CancellationTokenSource.Token.IsCancellationRequested)
            {
                logger.LogInformation("Speech abort requested; aborting speech from OpenAI TTS.");
                return;
            }

            //Processing is now done
            cancellableTask.IsWorkComplete = true;
            taskAvailable.Set();
        }

        private Task OutputSpeech(byte[] speechResult, CancellationTokenSource cancellationTokenSource)
        {
            Task result = Task.Run(async () =>
            {
                logger.LogInformation("Outputting speechResult to speakers.");
                using MemoryStream memoryStream = new MemoryStream(speechResult);
                using Mp3FileReader mp3FileReader = new Mp3FileReader(memoryStream);
                using WaveOutEvent waveOut = new WaveOutEvent();
                waveOut.Init(mp3FileReader);
                waveOut.Volume = 1f;

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

                    await Task.Delay(100);
                }

                logger.LogInformation("Finished outputting speechResult to speakers.");
            });

            return result;
        }
    }
}
