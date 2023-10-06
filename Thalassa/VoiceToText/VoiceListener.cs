using Microsoft.Extensions.Logging;

using StarmaidIntegrationComputer.Common.TasksAndExecution;

namespace StarmaidIntegrationComputer.Thalassa.VoiceToText
{
    public class VoiceListener
    {
        //I'm a little worried about using a queue and not passing more state around but this SHOULDN'T create race conditions or anything! 😅😅
        private readonly Queue<VoiceSession> runningSessions = new();
        private readonly ILogger<VoiceListener> logger;
        private readonly ILogger<IVoiceSession> sessionLogger;

        public bool IsRunning { get { return runningSessions.Count > 0; } }
        public List<Action> SessionStartingHandlers { get; } = new List<Action>();
        public List<Action> SessionCompleteHandlers { get; } = new List<Action>();

        public VoiceListener(ILogger<VoiceListener> logger, ILogger<IVoiceSession> sessionLogger)
        {
            this.logger = logger;
            this.sessionLogger = sessionLogger;
        }

        public Task<byte[]> StartListening()
        {
            if (IsRunning)
            {
                return Task.FromResult(new byte[0]);
            }

            VoiceSession session = new VoiceSession(sessionLogger);
            runningSessions.Enqueue(session);

            //This code is deprecated and is currently unreachable.
            if (runningSessions.Count > 1)
            {
                return session.ListeningTask;
            }

            session.ListeningTask.ContinueWith(continuationFunction: FireOnSessionCompleteIfNotCanceled());

            return StartSession(session);
        }

        public void StopListening()
        {
            if (runningSessions.Count > 0)
            {
                runningSessions.Peek().StopListening();
            }
        }

        private Func<Task<byte[]>, byte[]> FireOnSessionCompleteIfNotCanceled()
        {
            return t =>
            {
                if (t.Status != TaskStatus.Canceled)
                {
                    return OnSessionComplete().Result;
                }

                return new byte[0];
            };
        }

        private Task<byte[]> OnSessionComplete()
        {
            SessionCompleteHandlers.Invoke();
            logger.LogInformation("Voice session complete!");

            var result = runningSessions.Dequeue().ListeningTask;

            if (IsRunning)
            {
                logger.LogInformation("Starting the next voice session!");
                var nextRunningSession = runningSessions.Peek();
                StartSession(nextRunningSession);
            }

            logger.LogInformation("Returning on session complete!!");
            return result;
        }


        private Task<byte[]> StartSession(VoiceSession session)
        {
            SessionStartingHandlers.Invoke();
            return session.Start();
        }

        public void AbortCurrentListening()
        {
            foreach (IVoiceSession session in runningSessions)
            {
                session.Cancel();
            }

            runningSessions.Clear();
        }
    }
}
