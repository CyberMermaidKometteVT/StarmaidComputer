using System.DirectoryServices.ActiveDirectory;
using System.Threading;

using Microsoft.Extensions.Logging;

using Pv;

using StarmaidIntegrationComputer.Common.Settings;
using StarmaidIntegrationComputer.StarmaidSettings;
using StarmaidIntegrationComputer.Thalassa.VoiceToText;

namespace StarmaidIntegrationComputer.Thalassa.WakeWordProcessor
{
    internal class WakeWordProcessorPorcupine : WakeWordProcessorBase, IDisposable
    {
        private readonly string? porcupineAccessKey;
        private readonly string[] porcuppineKeywordFilePaths;
        private readonly Porcupine porcupineWakeWordListener;
        private readonly PvRecorder recorder;

        Task runningTask;
        CancellationTokenSource cancellationTokenSource;

        public WakeWordProcessorPorcupine(ILogger<WakeWordProcessorBase> logger, StreamerProfileSettings streamerProfileSettings, string? porcupineAccessKey, string[] porcupineKeywordFilePaths) : base(logger, streamerProfileSettings)
        {
            this.porcupineAccessKey = porcupineAccessKey;
            this.porcuppineKeywordFilePaths = porcupineKeywordFilePaths;


            porcupineWakeWordListener = Porcupine.FromKeywordPaths(porcupineAccessKey, porcupineKeywordFilePaths);
            recorder = PvRecorder.Create(porcupineWakeWordListener.FrameLength);
        }

        public override void StartListening()
        {
            recorder.Start();
            cancellationTokenSource = new CancellationTokenSource();
            IsListening = true;

            //TODO: Evaluate this more carefully, this one was vibe coded
            runningTask = Task.Run(PorcupineListen, cancellationTokenSource.Token);
        }

        public override void StopListening()
        {
            IsListening = false;
            this.cancellationTokenSource.Cancel();
        }

        public void PorcupineListen()
        {
            while (IsListening)
            {
                short[] frame = recorder.Read();
                int result = porcupineWakeWordListener.Process(frame);
                if (result >= 0)
                {
                    logger.LogInformation($"Wake word detected by {this.GetType().Name}!");

                    OnWakeWordHeard();
                }
            }
        }

        public override void Dispose()
        {
            porcupineWakeWordListener.Dispose();
            recorder.Dispose();
        }
    }
}
