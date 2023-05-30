using Microsoft.Extensions.Logging;

namespace StarmaidIntegrationComputer.Thalassa.VoiceToText
{
    public class VoiceListener
    {
        //I'm a little worried about using a queue and not passing more state around but this SHOULDN'T create race conditions or anything! 😅😅
        private readonly Queue<VoiceSession> runningSessions = new();
        private readonly ILogger<VoiceListener> logger;
        private readonly ILogger<IVoiceSession> sessionLogger;

        public bool IsRunning { get { return runningSessions.Count > 0; } }

        public VoiceListener(ILogger<VoiceListener> logger, ILogger<IVoiceSession> sessionLogger)
        {
            this.logger = logger;
            this.sessionLogger = sessionLogger;
        }

        public Task<byte[]> StartListening()
        {
            VoiceSession session = new VoiceSession(sessionLogger);
            runningSessions.Enqueue(session);

            if (runningSessions.Count > 1)
            {
                return session.ListeningTask;
            }

            session.ListeningTask.ContinueWith(t => OnSessionComplete().Result);

            return session.Start();
        }

        private Task<byte[]> OnSessionComplete()
        {
            logger.LogInformation("Voice session complete!");

            var result = runningSessions.Dequeue().ListeningTask;

            if (IsRunning)
            {
                logger.LogInformation("Starting the next voice session!");
                runningSessions.Peek().ListeningTask.Start();
            }

            logger.LogInformation("Returning on session complete!!");
            return result;
        }
    }
}
