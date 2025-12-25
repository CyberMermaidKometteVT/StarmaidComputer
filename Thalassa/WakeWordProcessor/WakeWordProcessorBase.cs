using Microsoft.Extensions.Logging;

using StarmaidIntegrationComputer.Common.Settings;

namespace StarmaidIntegrationComputer.Thalassa.WakeWordProcessor
{
    public abstract class WakeWordProcessorBase : IDisposable
    {
        protected readonly ILogger<WakeWordProcessorBase> logger;

        public bool IsListening { get; set; }

        public Action<string>? DisplayIfAble { get; set; }
        public Action OnWakeWordHeard { get; set; }
        public Action OnCancelListeningHeard { get; set; }
        public Action OnAbortCommandHeard { get; set; }

        public WakeWordProcessorBase(ILogger<WakeWordProcessorBase> logger, StreamerProfileSettings streamerProfileSettings)
        {
            this.logger = logger;
        }

        public abstract void StartListening();
        public abstract void StopListening();
        public abstract void Dispose();
    }
}
