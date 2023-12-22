
using Microsoft.Extensions.Logging;

using NAudio.Wave;

using StarmaidIntegrationComputer.Common.TasksAndExecution;

namespace StarmaidIntegrationComputer.Thalassa.VoiceToText
{
    internal class VoiceSession : IVoiceSession
    {
        const int maxSpeechLengthMilliseconds = 15 * 1000; //15 sec * milliseconds
        const int terminalSilenceLengthMilliseconds = 2 * 1000; // 2 sec * milliseconds

        public Task<byte[]> ListeningTask { get; private set; }

        private readonly ILogger<IVoiceSession> sessionLogger;
        private readonly IUiThreadDispatcher dispatcher;
        private readonly WaveIn? waveIn = new WaveIn();
        private WaveFileWriter waveFileWriter;

        private readonly TaskCompletionSource<byte[]> taskCompletionSource;
        public bool IsRunning { get; private set; } = false;


        private MemoryStream resultStream = new MemoryStream();
        private readonly DateTime startTime = DateTime.Now;
        private DateTime? silenceBegan = null;

        private object locker = new object();

        public VoiceSession(ILogger<IVoiceSession> sessionLogger, IUiThreadDispatcher dispatcher)
        {
            this.sessionLogger = sessionLogger;
            this.dispatcher = dispatcher;
            waveIn.WaveFormat = new WaveFormat(16000, 16, 1);
            waveFileWriter = new WaveFileWriter(resultStream, waveIn.WaveFormat);

            taskCompletionSource = new TaskCompletionSource<byte[]>(state: this);
            ListeningTask = taskCompletionSource.Task;
        }

        public Task<byte[]> Start()
        {
            try
            {
                sessionLogger.LogInformation("Starting voice session!");
                waveIn.DataAvailable += OnDataAvailable;

                IsRunning = true;
                waveIn.StartRecording();

                return ListeningTask;
            }
            catch (Exception)
            {
                sessionLogger.LogError("Voice session erroring out!!");
                resultStream.Dispose();
                waveIn.Dispose();

                throw;
            }
        }

        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            lock (locker)
            {
                if (!IsRunning)
                {
                    sessionLogger.LogInformation("Ignoring incoming voice data, we are trying to stop running!");
                    return;
                }

                if (ListeningTask.IsCanceled)
                {
                    StopListening();
                    return;
                }


                //Check if we should stop listening because we've been going on too long!
                var recordingDuration = DateTime.Now - startTime;
                if (recordingDuration.TotalMilliseconds > maxSpeechLengthMilliseconds)
                {
                    //The total duration of the recording has hit its maximum length; stop recording.
                    StopListening();
                    return;
                }

                //Wait for 2 seconds of silence
                if (IsSilence(e.Buffer))
                {
                    HandleSilence(e.Buffer);
                }
                else
                {
                    //We are NOT in the middle of a moment of silence!

                    sessionLogger.LogInformation("Voice session hearing sound!");

                    silenceBegan = null;


                    //Record the sound
                    waveFileWriter.Write(e.Buffer);
                }
            }
        }

        private void HandleSilence(byte[] buffer)
        {
            //We are in the middle of a moment of silence
            if (silenceBegan == null)
            {
                //If this is the first time we've observed silence this streak, note that!
                silenceBegan = DateTime.Now;
            }
            else
            {
                var silenceDuration = DateTime.Now - silenceBegan.Value;
                //Has the silence been going on to stop listening?
                sessionLogger.LogInformation($"Noting {silenceDuration.TotalMilliseconds}ms of silence.");
                if (silenceDuration.TotalMilliseconds > terminalSilenceLengthMilliseconds)
                {
                    //It HAS silent long enough to stop listening, so stop recording and call back with the result!
                    StopListening();
                }
                else
                {
                    //The silence is ongoing, this is not the first moment, but it hasn't been long enough to stop listening yet.  Consider recording the sound.

                    resultStream.Write(buffer);
                }
            }
        }

        public void StopListening()
        {
            if (!IsRunning)
            {
                return;
            }

            IsRunning = false;
            sessionLogger.LogInformation($"Stopping listening.");

            waveIn.StopRecording();
            if (this.ListeningTask.Status != TaskStatus.Canceled)
            {
                taskCompletionSource.SetResult(resultStream.ToArray());
            }
            dispatcher.ExecuteOnUiThread(waveIn.Dispose);
            resultStream.Dispose();
        }


        private bool IsSilence(byte[] buffer)
        {
            // Calculate the root mean square (RMS) of the audio data
            double rms = 0;
            for (int i = 0; i < buffer.Length; i += 2)
            {
                short sample = (short)(buffer[i + 1] << 8 | buffer[i]);
                rms += Math.Pow(sample / 32768.0, 2);
            }
            rms = Math.Sqrt(rms / (buffer.Length / 2));

            //This should be a LogTrace later!

            sessionLogger.LogInformation($"rms={rms}");

            // Check if the RMS is below a certain threshold (indicating silence)
            return rms < 0.008;
        }

        public void Cancel()
        {
            this.taskCompletionSource.SetCanceled();
        }
    }
}
